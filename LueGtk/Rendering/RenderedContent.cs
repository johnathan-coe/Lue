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
