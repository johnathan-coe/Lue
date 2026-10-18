namespace LueGtk;

public static class DocumentFiles
{
    public static string NextUntitled(string directory)
    {
        Directory.CreateDirectory(directory);
        for (var number = 1; ; number++)
        {
            var name = number == 1 ? "untitled.lue" : $"untitled-{number}.lue";
            var path = Path.Combine(directory, name);
            if (!File.Exists(path))
                return path;
        }
    }
}
