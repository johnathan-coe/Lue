namespace LueGtk;

public static class LineRenderer
{
    public const string Heading1 = "h1";
    public const string Heading2 = "h2";
    public const string Heading3 = "h3";
    public const string Body = "body";
    public const string Bullet = "li";
    public const string Image = "img";
    public const string Tex = "tex";

    public static readonly string[] Kinds = [Heading1, Heading2, Heading3, Body, Bullet, Tex, Image];

    public static string KindOf(string line)
    {
        if (line.StartsWith("###", StringComparison.Ordinal))
            return Heading3;
        if (line.StartsWith("##", StringComparison.Ordinal))
            return Heading2;
        if (line.StartsWith('#'))
            return Heading1;
        if (line.StartsWith('$'))
            return Tex;
        if (line.StartsWith('-'))
            return Bullet;
        if (line.StartsWith('^'))
            return Image;
        return Body;
    }

    public static string StripMarkers(string text)
    {
        while (text.Length > 0 && text[0] is '#' or ' ')
            text = text[1..];
        return text;
    }

    public static RenderedContent Render(string line, string workingDirectory, string texBackground = "", string texForeground = "")
    {
        var kind = KindOf(line);
        return kind switch
        {
            Heading1 or Heading2 or Heading3 or Body => RenderedContent.FromText(StripMarkers(line)),
            Bullet => RenderedContent.FromText("• " + (line.Length > 0 ? line[1..].Trim() : "")),
            Image => RenderImage(line, workingDirectory),
            Tex => RenderTex(line, texBackground, texForeground),
            _ => RenderedContent.FromText(StripMarkers(line))
        };
    }

    public static RenderedContent Render(string line, Theme theme, string workingDirectory)
    {
        var style = ElementStyle.For(theme, KindOf(line));
        return Render(line, workingDirectory, style.Background, style.Foreground);
    }

    static RenderedContent RenderImage(string line, string workingDirectory)
    {
        var filename = line.Length > 0 ? line[1..] : "";
        var path = Path.Combine(workingDirectory, filename);
        if (!File.Exists(path))
            return RenderedContent.FromText($"Image '{filename}' not found!");
        return RenderedContent.FromImagePath(path);
    }

    static RenderedContent RenderTex(string line, string background, string foreground)
    {
        var math = line.Length > 0 ? line[1..].Trim() : "";
        try
        {
            return RenderedContent.FromPng(TexRenderer.Render(math, background, foreground));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("TeXMath: " + ex.Message);
            return RenderedContent.FromText("TeXMath Rendering Error!");
        }
    }
}
