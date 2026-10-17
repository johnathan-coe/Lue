namespace LueGtk;

public sealed class Line
{
    public string Text { get; set; } = "";
    public bool Editing { get; set; }
}

/// <summary>
/// Document and caret rules ported from the Tkinter viewer: one line is edited at a time,
/// Enter inserts a line, leaving an empty line removes it, and blank lines are not stored.
/// </summary>
public sealed class EditSession
{
    public List<Line> Lines { get; } = [];
    public bool Unsaved { get; private set; }
    public string? FilePath { get; set; }
    public string WorkingDirectory { get; private set; } = Directory.GetCurrentDirectory();

    public event Action? Dirtied;

    public void Load(string path)
    {
        var full = Path.GetFullPath(path);
        Lines.Clear();
        foreach (var raw in File.ReadLines(full))
        {
            var text = raw.TrimEnd();
            if (text.Length == 0)
                continue;
            Lines.Add(new Line { Text = text });
        }

        FilePath = full;
        WorkingDirectory = Path.GetDirectoryName(full) ?? WorkingDirectory;
        Unsaved = false;
    }

    public void Save(string path)
    {
        var full = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var body = string.Join('\n', Lines.Select(line => line.Text).Where(text => text.Length > 0));
        File.WriteAllText(full, body + "\n");

        FilePath = full;
        if (!string.IsNullOrEmpty(directory))
            WorkingDirectory = directory;
        Unsaved = false;
    }

    public void NewDocument()
    {
        Lines.Clear();
        FilePath = null;
        Lines.Add(new Line { Editing = true });
        Unsaved = false;
    }

    public void EditFirst()
    {
        if (Lines.Count == 0)
            return;
        foreach (var line in Lines)
            line.Editing = false;
        Lines[0].Editing = true;
    }

    public void NoteEdit() => MarkDirty();

    public bool InsertAfter(Line prior)
    {
        var index = Lines.IndexOf(prior);
        if (index < 0)
            return false;

        var insertAt = index + 1;
        if (insertAt == Lines.Count && prior.Text.Length == 0)
            return false;

        Lines.Insert(insertAt, new Line());
        MarkDirty();
        Move(prior, +1);
        return true;
    }

    public void Move(Line item, int direction)
    {
        if (!item.Editing)
            return;

        var index = Lines.IndexOf(item);
        if (index < 0)
            return;

        var to = index + direction;
        if (to < 0)
            return;

        if (to < Lines.Count)
        {
            Lines[to].Editing = true;
            SetItem(item);
            return;
        }

        if (item.Text.Length == 0)
            return;

        InsertAfter(item);
    }

    public bool SetItem(Line item)
    {
        item.Editing = false;
        if (item.Text.Length == 0 && Lines.Remove(item))
            return true;
        return false;
    }

    public void Activate(Line item)
    {
        foreach (var line in Lines.ToList())
            SetItem(line);

        if (!Lines.Contains(item))
            return;

        foreach (var line in Lines)
            line.Editing = false;
        item.Editing = true;
    }

    void MarkDirty()
    {
        if (Unsaved)
            return;
        Unsaved = true;
        Dirtied?.Invoke();
    }
}
