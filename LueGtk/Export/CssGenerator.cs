namespace LueGtk;

/// <summary>
/// Ports the Tkinter-to-CSS export used by the HTML exporter.
/// </summary>
public static class CssGenerator
{
    static readonly Dictionary<string, string> PropertyNames = new(StringComparer.Ordinal)
    {
        ["bg"] = "background",
        ["fg"] = "color",
        ["justify"] = "text-align"
    };

    public static string Generate(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, StyleValue>> style,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, StyleValue>> packs)
    {
        var builder = new StringBuilder();
        foreach (var (tag, values) in style)
        {
            builder.Append(tag).Append(" {\n");
            builder.Append(StyleToCss(values));
            builder.Append("}\n\n");
        }

        foreach (var (tag, values) in packs)
        {
            builder.Append(tag).Append(" {\n");
            builder.Append(PackToCss(values));
            builder.Append("}\n\n");
        }

        return builder.ToString();
    }

    public static string StyleToCss(IReadOnlyDictionary<string, StyleValue> values)
    {
        var builder = new StringBuilder();
        foreach (var (key, value) in values)
        {
            if (key == "font")
                builder.Append(FontToCss(value));
            else if (key is "padx" or "pady")
                builder.Append(PaddingToCss(key, value, "padding"));
            else
                builder.Append(PropertyNames.TryGetValue(key, out var mapped) ? mapped : key).Append(": ").Append(value.Text).Append(";\n");
        }
        return builder.ToString();
    }

    public static string PackToCss(IReadOnlyDictionary<string, StyleValue> values)
    {
        var builder = new StringBuilder();
        foreach (var (key, value) in values)
        {
            if (key is "padx" or "pady")
                builder.Append(PaddingToCss(key, value, "margin"));
        }
        return builder.ToString();
    }

    public static string PaddingToCss(string key, StyleValue value, string prefix)
    {
        var (sideA, sideB) = key == "padx" ? ("left", "right") : ("top", "bottom");
        string first;
        string second;
        if (value.IsList)
        {
            first = value.Parts[0];
            second = value.Parts.Count > 1 ? value.Parts[1] : value.Parts[0];
        }
        else
        {
            first = second = value.Text;
        }

        return $"{prefix}-{sideA}: {first}px;\n{prefix}-{sideB}: {second}px;\n";
    }

    public static string FontToCss(StyleValue value)
    {
        if (value.Parts.Count == 2)
            return $"font: {value.Parts[1]}pt \"{value.Parts[0]}\";\n";
        if (value.Parts.Count == 3)
            return $"font: {value.Parts[2]} {value.Parts[1]}pt \"{value.Parts[0]}\";\n";

        Console.Error.WriteLine("WARNING: Unsupported tkinter font specification! " + value.Text);
        return "";
    }
}
