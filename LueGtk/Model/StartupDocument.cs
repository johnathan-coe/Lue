namespace LueGtk;

public static class StartupDocument
{
    /// <summary>
    /// Opens the welcome document, or the path passed on the command line.
    /// A missing path becomes an empty editable line and is written when the text changes.
    /// The welcome document is shown rendered; every other document starts on its first line.
    /// </summary>
    public static void OpenInitial(EditSession session, string? argument, string welcomePath)
    {
        var filename = string.IsNullOrWhiteSpace(argument)
            ? Path.GetFullPath(welcomePath)
            : Path.GetFullPath(argument);

        if (File.Exists(filename))
        {
            session.Load(filename);
        }
        else
        {
            session.Lines.Clear();
            session.FilePath = filename;
            session.Lines.Add(new Line { Editing = true });
        }

        if (session.Lines.Count == 0)
            session.Lines.Add(new Line { Editing = true });
        else if (!AppPaths.SamePath(filename, welcomePath))
            session.EditFirst();
    }
}
