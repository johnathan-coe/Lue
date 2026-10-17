using LueGtk;

namespace LueGtk.Ui;

static class ThemeCss
{
    public static string Build(Theme theme)
    {
        var frame = theme.Application.For("Frame");
        var body = ElementStyle.For(theme, LineRenderer.Body);
        var background = Color(frame, "bg") ?? body.Background;
        var foreground = body.Foreground;
        var (padXStart, padXEnd) = Pad(frame, "padx");
        var (padYTop, padYBottom) = Pad(frame, "pady");

        var css = new StringBuilder();
        css.Append(".lue-scroll, .lue-document, .lue-status {\n");
        css.Append("  background-color: ").Append(background).Append(";\n");
        css.Append("}\n");
        css.Append(".lue-document {\n");
        css.Append("  padding-left: ").Append(padXStart).Append("px;\n");
        css.Append("  padding-right: ").Append(padXEnd).Append("px;\n");
        css.Append("  padding-top: ").Append(padYTop).Append("px;\n");
        css.Append("  padding-bottom: ").Append(padYBottom).Append("px;\n");
        css.Append("}\n");
        css.Append(".lue-status {\n");
        css.Append("  color: ").Append(foreground).Append(";\n");
        css.Append("  padding: 6px 12px;\n");
        css.Append("  font-size: 11pt;\n");
        css.Append("}\n");
        css.Append(".lue-item textview {\n");
        css.Append("  background-color: transparent;\n");
        css.Append("  border-radius: 4px;\n");
        css.Append("}\n");
        css.Append(".lue-item textview:focus {\n");
        css.Append("  outline: 1px solid alpha(currentColor, 0.35);\n");
        css.Append("}\n");
        css.Append(".lue-item text selection {\n");
        css.Append("  background-color: alpha(currentColor, 0.28);\n");
        css.Append("  color: inherit;\n");
        css.Append("}\n");

        var kinds = new HashSet<string>(theme.Appearance.Sections.Keys, StringComparer.Ordinal);
        foreach (var kind in theme.Position.Sections.Keys)
            kinds.Add(kind);
        foreach (var kind in LineRenderer.Kinds)
            kinds.Add(kind);

        foreach (var kind in kinds.OrderBy(name => name, StringComparer.Ordinal))
        {
            var style = ElementStyle.For(theme, kind);
            css.Append(".lue-item.kind-").Append(kind).Append(" {\n");
            css.Append("  margin-top: ").Append(style.PadY.Top).Append("px;\n");
            css.Append("  margin-bottom: ").Append(style.PadY.Bottom).Append("px;\n");
            css.Append("  margin-left: ").Append(style.PadX.Start).Append("px;\n");
            css.Append("  margin-right: ").Append(style.PadX.End).Append("px;\n");
            css.Append("}\n");
            css.Append(".lue-item.kind-").Append(kind).Append(" label,\n");
            css.Append(".lue-item.kind-").Append(kind).Append(" textview,\n");
            css.Append(".lue-item.kind-").Append(kind).Append(" text {\n");
            if (style.Background.Length > 0)
                css.Append("  background-color: ").Append(style.Background).Append(";\n");
            if (style.Foreground.Length > 0)
            {
                css.Append("  color: ").Append(style.Foreground).Append(";\n");
                css.Append("  caret-color: ").Append(style.Foreground).Append(";\n");
            }
            css.Append(FontRule(style.Font));
            css.Append("}\n");
        }

        return css.ToString();
    }

    static string FontRule(StyleValue? font)
    {
        if (font is not { } value || value.Parts.Count < 2)
            return "";

        var family = value.Parts[0].Replace("\"", "", StringComparison.Ordinal);
        var size = value.Parts[1];
        var weight = "normal";
        var slant = "normal";
        if (value.Parts.Count >= 3)
        {
            var flags = value.Parts[2].ToLowerInvariant();
            if (flags.Contains("bold", StringComparison.Ordinal))
                weight = "bold";
            if (flags.Contains("italic", StringComparison.Ordinal))
                slant = "italic";
        }

        var generic = IsSerif(family) ? "serif" : "sans-serif";
        return $"  font-family: \"{family}\", {generic};\n  font-size: {size}pt;\n  font-weight: {weight};\n  font-style: {slant};\n";
    }

    static bool IsSerif(string family)
    {
        return family.Contains("Serif", StringComparison.OrdinalIgnoreCase) ||
               family.Contains("Roman", StringComparison.OrdinalIgnoreCase) ||
               family.Contains("Modern", StringComparison.OrdinalIgnoreCase) ||
               family.Contains("Times", StringComparison.OrdinalIgnoreCase);
    }

    static string? Color(IReadOnlyDictionary<string, StyleValue> values, string key)
    {
        return values.TryGetValue(key, out var value) && value.Text.Length > 0 ? value.Text : null;
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
