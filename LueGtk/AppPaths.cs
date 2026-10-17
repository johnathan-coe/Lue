namespace LueGtk;

/// <summary>
/// Locates the welcome document, logo, and the default stylesheet used by HTML export.
/// Prefers files copied next to the executable, then walks up toward the repository root.
/// </summary>
public static class AppPaths
{
    public static string Root { get; } = FindRoot();

    public static string Welcome => Path.Combine(Root, "welcome.lue");

    public static string ThemesDirectory => Path.Combine(Root, "themes");

    public static string DefaultTheme => Path.Combine(ThemesDirectory, "ms");

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
