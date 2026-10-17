namespace LueGtk;

public static class HtmlExporter
{
    public const string Name = "HTML";

    public static string Write(EditSession session, Theme theme, Func<string, byte[]>? convertImage = null)
    {
        return Write(session.Lines.Select(line => line.Text).ToList(), theme, session.WorkingDirectory, convertImage);
    }

    public static string Write(
        IReadOnlyList<string> lines,
        Theme theme,
        string workingDirectory,
        Func<string, byte[]>? convertImage = null)
    {
        var outputDirectory = Path.Combine(workingDirectory, "rendered");
        var imageDirectory = Path.Combine(outputDirectory, "img");
        Directory.CreateDirectory(imageDirectory);

        var style = new Dictionary<string, IReadOnlyDictionary<string, StyleValue>>(StringComparer.Ordinal)
        {
            ["body"] = theme.Application.For("Frame")
        };
        var packs = new Dictionary<string, IReadOnlyDictionary<string, StyleValue>>(StringComparer.Ordinal);
        var body = new StringBuilder();
        var imageCount = 0;

        foreach (var line in lines)
        {
            if (line.Length == 0)
                continue;

            var kind = LineRenderer.KindOf(line);
            var rendered = LineRenderer.Render(line, theme, workingDirectory);
            style["." + kind] = theme.Appearance.For(kind);
            packs["." + kind] = theme.Position.For(kind);

            if (!rendered.IsImage)
            {
                body.Append("<p class=\"").Append(kind).Append("\">");
                body.Append(Escape(rendered.Text ?? ""));
                body.Append("</p>\n");
                continue;
            }

            var relative = $"img/{imageCount}.png";
            File.WriteAllBytes(Path.Combine(outputDirectory, relative), PngBytes(rendered, convertImage));
            body.Append("<img class=\"").Append(kind).Append("\" src=\"").Append(relative).Append("\">\n");
            imageCount++;
        }

        var html = "<html>\n<head>\n<meta charset=\"UTF-8\">\n<link rel=\"stylesheet\" type=\"text/css\" href=\"./index.css\" media=\"screen\" />\n</head>\n<body>\n"
            + body
            + "</body>\n</html>";

        var htmlPath = Path.Combine(outputDirectory, "index.html");
        File.WriteAllText(htmlPath, html);
        File.WriteAllText(Path.Combine(outputDirectory, "index.css"), CssGenerator.Generate(style, packs));
        return htmlPath;
    }

    static byte[] PngBytes(RenderedContent rendered, Func<string, byte[]>? convertImage)
    {
        if (rendered.Png is not null)
            return rendered.Png;

        var path = rendered.ImagePath ?? throw new InvalidOperationException("Image content is missing a path.");
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return File.ReadAllBytes(path);
        if (convertImage is null)
            throw new InvalidOperationException($"Cannot convert '{path}' to PNG without an image decoder.");
        return convertImage(path);
    }

    static string Escape(string text)
    {
        return text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }
}
