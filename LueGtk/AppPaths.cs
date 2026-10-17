namespace LueGtk;

/// <summary>
/// Locates the shared themes, welcome document, and logo.
/// Prefers files copied next to the executable, then walks up toward the repository root.
/// </summary>
public static class AppPaths
{
    static readonly string[] PreferredThemes = ["ms", "latex", "lukesmith", "lowriter"];

    public static string Root { get; } = FindRoot();

    public static string Welcome => Path.Combine(Root, "welcome.lue");

    public static string ThemesDirectory => Path.Combine(Root, "themes");

    public static string DefaultTheme => Path.Combine(ThemesDirectory, "ms");

    public static IReadOnlyList<string> ThemeDirectories()
    {
        if (!Directory.Exists(ThemesDirectory))
            return [];

        var found = Directory.GetDirectories(ThemesDirectory)
            .Where(dir => File.Exists(Path.Combine(dir, "application.ini")))
            .ToList();

        return found
            .OrderBy(dir =>
            {
                var index = Array.IndexOf(PreferredThemes, Path.GetFileName(dir));
                return index < 0 ? PreferredThemes.Length : index;
            })
            .ThenBy(dir => Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool SamePath(string left, string right)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), comparison);
    }

    static string FindRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "themes", "ms", "application.ini")) &&
                    File.Exists(Path.Combine(dir.FullName, "welcome.lue")))
                    return dir.FullName;
                dir = dir.Parent;
            }
        }

        return AppContext.BaseDirectory;
    }
}
