namespace LueGtk;

/// <summary>
/// One parsed stylesheet. Every named section already includes the DEFAULT keys,
/// and unknown section names fall back to DEFAULT the way the Tk loader does.
/// </summary>
public sealed class StyleBook
{
    public IReadOnlyDictionary<string, StyleValue> Fallback { get; }
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, StyleValue>> Sections { get; }

    StyleBook(
        IReadOnlyDictionary<string, StyleValue> fallback,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, StyleValue>> sections)
    {
        Fallback = fallback;
        Sections = sections;
    }

    public static StyleBook Load(string path)
    {
        var raw = IniFile.Parse(path);
        return FromSections(raw);
    }

    public static StyleBook FromSections(Dictionary<string, Dictionary<string, StyleValue>> raw)
    {
        var fallback = raw.TryGetValue("DEFAULT", out var defaults)
            ? new Dictionary<string, StyleValue>(defaults, StringComparer.Ordinal)
            : new Dictionary<string, StyleValue>(StringComparer.Ordinal);

        var sections = new Dictionary<string, IReadOnlyDictionary<string, StyleValue>>(StringComparer.Ordinal);
        foreach (var (name, values) in raw)
        {
            if (name == "DEFAULT")
                continue;

            var merged = new Dictionary<string, StyleValue>(fallback, StringComparer.Ordinal);
            foreach (var (key, value) in values)
                merged[key] = value;
            sections[name] = merged;
        }

        return new StyleBook(fallback, sections);
    }

    public IReadOnlyDictionary<string, StyleValue> For(string section)
    {
        return Sections.TryGetValue(section, out var found) ? found : Fallback;
    }
}
