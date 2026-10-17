namespace LueGtk;

/// <summary>
/// A small INI reader compatible with the theme files consumed by the Python app.
/// Keys are lower-cased, DEFAULT is preserved for the caller to merge, and values that
/// contain a comma are split the same way Tkinter's theme loader splits them.
/// </summary>
public static class IniFile
{
    public static Dictionary<string, Dictionary<string, StyleValue>> Parse(string path)
    {
        return ParseText(File.ReadAllText(path));
    }

    public static Dictionary<string, Dictionary<string, StyleValue>> ParseText(string text)
    {
        var sections = new Dictionary<string, Dictionary<string, StyleValue>>(StringComparer.Ordinal);
        Dictionary<string, StyleValue>? current = null;

        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } raw)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
                continue;

            if (line.StartsWith('[') && line.EndsWith(']') && line.Length > 2)
            {
                var name = line[1..^1].Trim();
                if (!sections.TryGetValue(name, out current))
                {
                    current = new Dictionary<string, StyleValue>(StringComparer.Ordinal);
                    sections[name] = current;
                }
                continue;
            }

            if (current is null)
                continue;

            var separator = line.IndexOfAny(['=', ':']);
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim().ToLowerInvariant();
            var value = line[(separator + 1)..].Trim();
            current[key] = StyleValue.Parse(value);
        }

        return sections;
    }
}

public readonly struct StyleValue
{
    public string Text { get; }
    public bool IsList { get; }
    public IReadOnlyList<string> Parts { get; }

    StyleValue(string text, bool isList, IReadOnlyList<string> parts)
    {
        Text = text;
        IsList = isList;
        Parts = parts;
    }

    public static StyleValue Parse(string raw)
    {
        if (!raw.Contains(','))
            return new StyleValue(raw, false, [raw]);

        var parts = raw.Split(',').Select(part => part.Trim()).ToArray();
        return new StyleValue(raw, true, parts);
    }

    public static StyleValue Scalar(string text) => new(text, false, [text]);

    public static StyleValue List(params string[] parts) => new(string.Join(',', parts), true, parts);
}
