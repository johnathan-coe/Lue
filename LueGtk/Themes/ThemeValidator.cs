namespace LueGtk;

public static class ThemeValidator
{
    public static void ValidateInternal(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, StyleValue>> sections,
        IReadOnlyCollection<string>? fontFamilies)
    {
        if (fontFamilies is not null)
        {
            foreach (var (name, section) in sections)
            {
                if (!section.TryGetValue("font", out var font) || font.Parts.Count == 0)
                    continue;

                var family = font.Parts[0];
                if (!HasFont(fontFamilies, family))
                    Console.Error.WriteLine($"Warning! Font: '{family}' used in style class '{name}' not found!");
            }
        }

        if (sections.Count <= 1)
            return;

        var names = sections.Keys.ToList();
        for (var i = 1; i < names.Count; i++)
        {
            var previous = names[i - 1];
            var current = names[i];
            var left = sections[previous].Keys.ToHashSet(StringComparer.Ordinal);
            var right = sections[current].Keys.ToHashSet(StringComparer.Ordinal);
            if (left.SetEquals(right))
                continue;

            left.SymmetricExceptWith(right);
            var diff = string.Join(", ", left.OrderBy(key => key, StringComparer.Ordinal));
            throw new InvalidDataException(
                $"Attributes: {diff} must be defined for all styles (Discrepancy between '{previous}', '{current}'). Try DEFAULT?");
        }
    }

    public static void ValidateSwitch(Theme from, Theme to)
    {
        ValidatePair(from.Appearance, to.Appearance);
        ValidatePair(from.Position, to.Position);
        ValidatePair(from.Application, to.Application);
    }

    static void ValidatePair(StyleBook from, StyleBook to)
    {
        var merged = new Dictionary<string, IReadOnlyDictionary<string, StyleValue>>(from.Sections, StringComparer.Ordinal);
        foreach (var (name, section) in to.Sections)
            merged["new" + name] = section;
        ValidateInternal(merged, fontFamilies: null);
    }

    static bool HasFont(IReadOnlyCollection<string> families, string family)
    {
        if (Contains(families, family))
            return true;

        var words = family.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
            return false;

        var shorter = string.Join(' ', words.Take(words.Length - 1));
        return Contains(families, shorter);
    }

    static bool Contains(IReadOnlyCollection<string> families, string family)
    {
        foreach (var candidate in families)
        {
            if (string.Equals(candidate, family, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
