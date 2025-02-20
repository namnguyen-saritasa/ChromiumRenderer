using ChromiumRenderer;
using PuppeteerSharp;

namespace Test;

class Program
{
    public static async Task Main()
    {
        {
            Console.WriteLine("Downloading chrome");
            var fetcher = await (new BrowserFetcher(SupportedBrowser.Chrome).DownloadAsync(BrowserTag.Stable));
            var executablePath = fetcher.GetExecutablePath();
            Console.WriteLine($"Headless chrome path: {executablePath}");
            Console.WriteLine("Launching");
            using var browser = await Puppeteer.LaunchAsync(new()
            {
                Headless = true,
                ExecutablePath = executablePath
            });
            Console.WriteLine("New page");
            using var page = await browser.NewPageAsync();
        }
        Console.WriteLine("Initializing renderer");
        await using var renderer = new HtmlRenderer();
        Console.WriteLine("Rendering");
        var content = await renderer.RenderPdf("<html><body><h1>Hello, PDF!</h1></body></html>");
        Console.WriteLine("Rendered!");
        _ = content;
    }
}