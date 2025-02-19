using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace ChromiumRenderer;

public class HtmlRenderer : IDisposable, IAsyncDisposable
{
    private readonly LaunchOptions launchOptions;
    private IBrowser? browser;
    
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
#if INCLUDED_HEADLESS_CHROME_WINAMD64
        if (OperatingSystem.IsWindowsVersionAtLeast(7) && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            return "runtimes-cache/win-x64/native/chrome-headless-shell.exe";
        }
#endif
        
#if INCLUDED_HEADLESS_CHROME_LINUXAMD64
        if (OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            return "runtimes-cache/linux-x64/native/chrome-headless-shell";
        }
#endif
#if INCLUDED_HEADLESS_CHROME_OSXARM64
        if (OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            return "runtimes-cache/osx-arm64/native/chrome-headless-shell";
        }
#endif
        throw new NotSupportedException($"OS not supported: {Environment.OSVersion}, Pointer width: {Unsafe.SizeOf<nuint>()}");
    }

    public HtmlRenderer(HtmlRendererConfigurations? configurations = null)
    {
        if (configurations == null || string.IsNullOrEmpty(configurations.ExecutablePath))
        {
            var executablePath = Path.Join(AssemblyDirectory, GetRelativePrepackedExecutablePath());
            CheckExecutableExists(executablePath);
            launchOptions = new()
            {
                Headless = true,
                ExecutablePath = executablePath,
                Args = ["--disable-gpu", "--no-sandbox"]
            };

            return;
        }

        launchOptions = new()
        {
            Headless = configurations.Headless,
            ExecutablePath = configurations.ExecutablePath,
        };
    }

    public HtmlRenderer(IOptions<HtmlRendererConfigurations> options)
        : this(options.Value)
    {
    }

    private async Task<IBrowser> EnsureInitialized()
    {
        // This fetch is atomic
        var b = browser;
        if (b != null)
        {
            return b;
        }
        
        b = await Puppeteer.LaunchAsync(launchOptions);
        var oldValue = Interlocked.CompareExchange(ref browser, b, null);
        if (oldValue == null)
        {
            return b;
        }
        await b.DisposeAsync();
        return oldValue;

    }

    public async Task RenderPdf(string content, Stream stream, PdfOptions? pdfOptions = null)
    {
        var b = await EnsureInitialized();
        await using var page = await b.NewPageAsync();
        await page.SetContentAsync(content);
        var s = await page.PdfStreamAsync(pdfOptions ?? new() { Format = PaperFormat.A4 });
        await s.CopyToAsync(stream);
    }
    
    public async Task<byte[]> RenderPdf(string content, PdfOptions? pdfOptions = null)
    {
        var b = await EnsureInitialized();
        await using var page = await b.NewPageAsync();
        await page.SetContentAsync(content);
        return await page.PdfDataAsync(pdfOptions ?? new() { Format = PaperFormat.A4 });
    }

    public void Dispose()
    {
        IBrowser? b = null;
        b = Interlocked.Exchange(ref browser, b);
        b?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        IBrowser? b = null;
        b = Interlocked.Exchange(ref browser, b);
        if (b != null)
        {
            return b.DisposeAsync();
        }
        
        return ValueTask.CompletedTask;
    }
}