namespace LueGtk;

/// <summary>
/// Sidebar contents: .lue files, and folders that contain a .lue file somewhere inside.
/// Names that start with a dot are skipped.
/// </summary>
public static class LueFolderListing
{
    public readonly record struct Entry(string Path, bool IsDirectory);

    public static bool IsLueFile(string path)
    {
        return path.EndsWith(".lue", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<Entry> List(string directory)
    {
        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(directory);
        }
        catch (Exception)
        {
            return [];
        }

        return entries
            .Where(entry => !Path.GetFileName(entry).StartsWith('.'))
            .Select(entry => (Path: entry, IsDirectory: Directory.Exists(entry)))
            .Where(entry => entry.IsDirectory ? ContainsLue(entry.Path) : IsLueFile(entry.Path))
            .OrderBy(entry => entry.IsDirectory ? 0 : 1)
            .ThenBy(entry => Path.GetFileName(entry.Path), StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => Path.GetFileName(entry.Path), StringComparer.Ordinal)
            .Select(entry => new Entry(entry.Path, entry.IsDirectory))
            .ToList();
    }

    static bool ContainsLue(string directory)
    {
        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                if (Path.GetFileName(entry).StartsWith('.'))
                    continue;
                if (Directory.Exists(entry))
                {
                    if (ContainsLue(entry))
                        return true;
                }
                else if (IsLueFile(entry))
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            return false;
        }

        return false;
    }
}
