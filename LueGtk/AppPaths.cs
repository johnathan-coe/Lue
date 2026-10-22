namespace LueGtk;

/// <summary>
/// Locates the welcome document and logo.
/// Prefers files copied next to the executable, then walks up toward the repository root.
/// </summary>
public static class AppPaths
{
    public static string Root { get; } = FindRoot();

    public static string Welcome => Path.Combine(Root, "welcome.lue");

    /// <summary>
    /// The directory the process was started in. An AppImage hook records that
    /// path in OWD before anything else runs, so a later directory change does
    /// not hide it. A missing or relative OWD falls back to the current directory.
    /// </summary>
    public static string ResolveLaunchDirectory(string? originalWorkingDirectory, string currentDirectory)
    {
        var current = Path.GetFullPath(currentDirectory);
        if (!string.IsNullOrWhiteSpace(originalWorkingDirectory))
        {
            var candidate = Path.IsPathRooted(originalWorkingDirectory)
                ? Path.GetFullPath(originalWorkingDirectory)
                : Path.GetFullPath(Path.Combine(current, originalWorkingDirectory));
            if (Directory.Exists(candidate))
                return candidate;
        }

        return current;
    }

    /// <summary>
    /// With no path on the command line, the sidebar stays in the launch
    /// directory. A path argument opens that file's folder instead.
    /// </summary>
    public static string SidebarDirectoryForLaunch(string? argument, string launchDirectory, string openedFileDirectory)
    {
        if (!string.IsNullOrWhiteSpace(argument) &&
            !string.IsNullOrEmpty(openedFileDirectory) &&
            Directory.Exists(openedFileDirectory))
            return Path.GetFullPath(openedFileDirectory);

        return Path.GetFullPath(launchDirectory);
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
        var share = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "share", "lue"));
        foreach (var start in new[] { AppContext.BaseDirectory, share, Directory.GetCurrentDirectory() })
        {
            // Prefer the outermost match so a copied welcome file in the build
            // output does not hide the repository that contains it.
            string? found = null;
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "themes", "ms", "application.ini")) &&
                    File.Exists(Path.Combine(dir.FullName, "welcome.lue")))
                    found = dir.FullName;
                dir = dir.Parent;
            }

            if (found is not null)
                return found;
        }

        return AppContext.BaseDirectory;
    }
}
