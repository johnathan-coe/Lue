namespace LueGtk;

/// <summary>
/// Renders a math expression to PNG with a local LaTeX install, matching the Python
/// extension's sympy preview. The result is cached by expression and colors.
/// When LaTeX is not installed the caller shows the same error string as the Tk app.
/// </summary>
public static class TexRenderer
{
    static readonly Dictionary<(string Math, string Bg, string Fg), byte[]> Cache = new();

    public static byte[] Render(string math, string background, string foreground)
    {
        var key = (math, background, foreground);
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var cached))
                return cached;
        }

        var png = RenderUncached(math, background, foreground);
        lock (Cache)
            Cache[key] = png;
        return png;
    }

    static byte[] RenderUncached(string math, string background, string foreground)
    {
        var pdflatex = FindTool("pdflatex");
        if (pdflatex is null)
            throw new InvalidOperationException("pdflatex was not found on PATH.");

        var directory = Directory.CreateTempSubdirectory("lue-tex-");
        try
        {
            var texPath = Path.Combine(directory.FullName, "formula.tex");
            File.WriteAllText(texPath, BuildDocument(math, background, foreground));

            var (code, log) = Run(pdflatex, directory.FullName, "-no-shell-escape", "-interaction=nonstopmode", "-halt-on-error", "formula.tex");
            if (code != 0)
                throw new InvalidOperationException("pdflatex failed.\n" + Tail(log));

            var pdf = Path.Combine(directory.FullName, "formula.pdf");
            var png = Path.Combine(directory.FullName, "formula.png");
            ConvertPdf(directory.FullName, pdf, png);
            return File.ReadAllBytes(png);
        }
        finally
        {
            try { directory.Delete(recursive: true); }
            catch (IOException) { }
        }
    }

    public static string BuildDocument(string math, string background, string foreground)
    {
        var colors = new StringBuilder();
        if (Hex(background) is { } bg)
        {
            colors.Append("\\definecolor{pagebg}{HTML}{").Append(bg).AppendLine("}");
            colors.AppendLine("\\pagecolor{pagebg}");
        }
        if (Hex(foreground) is { } fg)
        {
            colors.Append("\\definecolor{ink}{HTML}{").Append(fg).AppendLine("}");
            colors.AppendLine("\\color{ink}");
        }

        return string.Join('\n',
            "\\documentclass{article}",
            "\\usepackage{amsmath,amssymb}",
            "\\usepackage{xcolor}",
            "\\usepackage[active,tightpage]{preview}",
            "\\pagestyle{empty}",
            "\\begin{document}",
            colors.ToString().TrimEnd(),
            "\\begin{preview}",
            "$\\displaystyle " + math + "$",
            "\\end{preview}",
            "\\end{document}",
            "");
    }

    static void ConvertPdf(string directory, string pdf, string png)
    {
        var pdftocairo = FindTool("pdftocairo");
        if (pdftocairo is not null)
        {
            var (code, log) = Run(pdftocairo, directory, "-png", "-r", "192", "-singlefile", pdf, Path.Combine(directory, "formula"));
            if (code == 0 && File.Exists(png))
                return;
            throw new InvalidOperationException("pdftocairo failed.\n" + Tail(log));
        }

        var magick = FindTool("magick") ?? FindTool("convert");
        if (magick is null)
            throw new InvalidOperationException("pdftocairo or ImageMagick is required to rasterize TeX output.");

        var (convertCode, convertLog) = Run(magick, directory, "-density", "192", pdf, png);
        if (convertCode != 0 || !File.Exists(png))
            throw new InvalidOperationException("ImageMagick failed.\n" + Tail(convertLog));
    }

    static string? Hex(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return null;
        return color.StartsWith('#') ? color[1..] : color;
    }

    public static string? FindTool(string name)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
            return null;

        var extensions = OperatingSystem.IsWindows() ? new[] { ".exe", ".cmd", ".bat", "" } : new[] { "" };
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(directory, name + extension);
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        return null;
    }

    static (int Code, string Log) Run(string fileName, string workingDirectory, params string[] arguments)
    {
        var start = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = start };
        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(20_000))
        {
            try { process.Kill(entireProcessTree: true); }
            catch (Exception) { }
            throw new TimeoutException(fileName + " timed out.");
        }

        return (process.ExitCode, stdout.GetAwaiter().GetResult() + "\n" + stderr.GetAwaiter().GetResult());
    }

    static string Tail(string log)
    {
        var lines = log.Split('\n');
        return string.Join('\n', lines.TakeLast(20));
    }
}
