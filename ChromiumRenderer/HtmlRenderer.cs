using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace ChromiumRenderer;

/// <summary>
/// HTML to PDF renderer.
/// </summary>
public class HtmlRenderer : CriticalFinalizerObject, IDisposable, IAsyncDisposable
{
    private readonly LaunchOptions launchOptions;
    private IBrowser? browser;
    private bool disposed;
    
    private static string AssemblyDirectory
    {
        get
        {
            var codeBase = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(codeBase) ?? throw new UnreachableException();
        }
    }

    private static void CheckExecutableExists(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Missing chromium executable: {fileName}");
        }
    }

    private static string GetRelativePrepackedExecutablePath()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(7) && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            return "runtimes-cache/win-x64/native/chrome-headless-shell.exe";
        }

        if (OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            return "runtimes-cache/linux-x64/native/chrome-headless-shell";
        }

        if (OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            return "runtimes-cache/osx-arm64/native/chrome-headless-shell";
        }

        throw new NotSupportedException($"OS not supported: {Environment.OSVersion}, Pointer width: {Unsafe.SizeOf<nuint>()}");
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="launchOptions">Launch options.</param>
    /// <remarks>
    /// If the launch options or its executable path is not provided,
    /// HtmlRenderer will use the bundled ChromiumRenderer.
    /// </remarks>
    public HtmlRenderer(LaunchOptions? launchOptions = null)
    {
        if (launchOptions == null || string.IsNullOrEmpty(launchOptions.ExecutablePath))
        {
            var executablePath = Path.Join(AssemblyDirectory, GetRelativePrepackedExecutablePath());
            CheckExecutableExists(executablePath);
            this.launchOptions = new()
            {
                Headless = true,
                ExecutablePath = executablePath,
                Args = ["--disable-gpu", "--no-sandbox"]
            };

            return;
        }

        this.launchOptions = launchOptions;
    }

    private async Task<IBrowser> EnsureInitialized()
    {
        // This fetch is atomic
        var b = browser;
        if (b != null)
        {
            return b;
        }

        ObjectDisposedException.ThrowIf(disposed, this);
        b = await Puppeteer.LaunchAsync(launchOptions);
        var oldValue = Interlocked.CompareExchange(ref browser, b, null);
        if (oldValue == null)
        {
            return b;
        }
        await b.DisposeAsync();
        return oldValue;
    }

    /// <summary>
    /// Initialize the browser.
    /// </summary>
    public Task Initialize()
    {
        return EnsureInitialized();
    }

    /// <summary>
    /// Render HTML to PDF.
    /// </summary>
    /// <param name="content">HTML content.</param>
    /// <param name="stream">Output stream/</param>
    /// <param name="pdfOptions">Render options.</param>
    public async Task RenderPdf(string content, Stream stream, PdfOptions? pdfOptions = null)
    {
        var b = await EnsureInitialized();
        await using var page = await b.NewPageAsync();
        await page.SetContentAsync(content);
        var s = await page.PdfStreamAsync(pdfOptions ?? new() { Format = PaperFormat.A4 });
        await s.CopyToAsync(stream);
    }

    /// <summary>
    /// Render HTML to PDF.
    /// </summary>
    /// <param name="content">HTML content.</param>
    /// <param name="pdfOptions">Render options.</param>
    /// <returns>PDF content as a byte array.</returns>
    public async Task<byte[]> RenderPdf(string content, PdfOptions? pdfOptions = null)
    {
        var b = await EnsureInitialized();
        await using var page = await b.NewPageAsync();
        await page.SetContentAsync(content);
        return await page.PdfDataAsync(pdfOptions ?? new() { Format = PaperFormat.A4 });
    }

    /// <inheritdoc />
    ~HtmlRenderer()
    {
        Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        disposed = true; // Atomic
        IBrowser? b = null;
        b = Interlocked.Exchange(ref browser, b);
        b?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        disposed = true; // Atomic
        IBrowser? b = null;
        b = Interlocked.Exchange(ref browser, b);
        if (b != null)
        {
            return b.DisposeAsync();
        }

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}