namespace LueGtk;

public sealed class RenderedContent
{
    public string? Text { get; private init; }
    public string? ImagePath { get; private init; }
    public byte[]? Png { get; private init; }

    public bool IsImage => ImagePath is not null || Png is not null;

    public static RenderedContent FromText(string text) => new() { Text = text };

    public static RenderedContent FromImagePath(string path) => new() { ImagePath = path };

    public static RenderedContent FromPng(byte[] png) => new() { Png = png };
}

public sealed class ElementStyle
{
    readonly IReadOnlyDictionary<string, StyleValue> _appearance;
    readonly IReadOnlyDictionary<string, StyleValue> _position;

    public ElementStyle(
        IReadOnlyDictionary<string, StyleValue> appearance,
        IReadOnlyDictionary<string, StyleValue> position)
    {
        _appearance = appearance;
        _position = position;
    }

    public string Background => Read(_appearance, "bg");
    public string Foreground => Read(_appearance, "fg");
    public string Justify => Read(_appearance, "justify") is { Length: > 0 } justify ? justify : "left";
    public string Anchor => Read(_position, "anchor") is { Length: > 0 } anchor ? anchor : "w";
    public StyleValue? Font => _appearance.TryGetValue("font", out var font) ? font : null;
    public (int Start, int End) PadX => Pad(_position, "padx");
    public (int Top, int Bottom) PadY => Pad(_position, "pady");

    public static ElementStyle For(Theme theme, string kind)
    {
        return new ElementStyle(theme.Appearance.For(kind), theme.Position.For(kind));
    }

    static string Read(IReadOnlyDictionary<string, StyleValue> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value.Text : "";
    }

    static (int Start, int End) Pad(IReadOnlyDictionary<string, StyleValue> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
            return (0, 0);

        if (value.IsList && value.Parts.Count >= 2 &&
            int.TryParse(value.Parts[0], out var start) &&
            int.TryParse(value.Parts[1], out var end))
            return (start, end);

        var scalar = value.IsList ? value.Parts[0] : value.Text;
        return int.TryParse(scalar, out var both) ? (both, both) : (0, 0);
    }
}
