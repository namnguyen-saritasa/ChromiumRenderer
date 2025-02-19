using ChromiumRenderer;

namespace Test;

class Program
{
    public static async Task Main()
    {
        Console.WriteLine("Initializing renderer");
        await using var renderer = new HtmlRenderer();
        Console.WriteLine("Rendering");
        var content = await renderer.RenderPdf("<html><body><h1>Hello, PDF!</h1></body></html>");
        Console.WriteLine("Rendered!");
        _ = content;
    }
}