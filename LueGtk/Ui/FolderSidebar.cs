using Gtk;

namespace LueGtk.Ui;

/// <summary>
/// A single-level folder list. Folders open in place, and text documents are handed
/// to the editor. Hidden names are skipped.
/// </summary>
sealed class FolderSidebar
{
    static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".lue", ".md", ".markdown", ".txt", ".css", ".html", ".ini",
        ".py", ".cs", ".json", ".xml", ".svg", ".log"
    };

    readonly Dictionary<ListBoxRow, string> _paths = [];
    readonly ListBox _list;
    readonly Label _title;
    readonly Button _up;
    bool _suppress;

    public Widget Root { get; }
    public string Directory { get; private set; }

    public event Action<string>? FileChosen;
    public event Action<string>? Rejected;

    public FolderSidebar(string directory)
    {
        Directory = directory;

        _up = Button.NewFromIconName("go-up-symbolic");
        _up.TooltipText = "Parent folder";
        _up.AddCssClass("flat");
        _up.OnClicked += (_, _) => GoUp();

        _title = Label.New("");
        _title.Hexpand = true;
        _title.Halign = Align.Start;
        _title.Xalign = 0;
        _title.Ellipsize = Pango.EllipsizeMode.Start;
        _title.AddCssClass("heading");

        var header = Box.New(Orientation.Horizontal, 4);
        header.MarginStart = 6;
        header.MarginEnd = 6;
        header.MarginTop = 6;
        header.MarginBottom = 6;
        header.Append(_up);
        header.Append(_title);

        _list = ListBox.New();
        _list.SelectionMode = SelectionMode.Browse;
        _list.ActivateOnSingleClick = true;
        _list.AddCssClass("navigation-sidebar");
        _list.OnRowActivated += (_, args) => Activate(args.Row);

        var scroll = ScrolledWindow.New();
        scroll.Child = _list;
        scroll.Hexpand = true;
        scroll.Vexpand = true;
        scroll.SetPolicy(PolicyType.Never, PolicyType.Automatic);

        var content = Box.New(Orientation.Vertical, 0);
        content.WidthRequest = 248;
        content.Hexpand = false;
        content.Vexpand = true;
        content.AddCssClass("sidebar");
        content.Append(header);
        content.Append(scroll);

        var sidebar = Box.New(Orientation.Horizontal, 0);
        sidebar.Append(content);
        sidebar.Append(Separator.New(Orientation.Vertical));
        Root = sidebar;

        ShowDirectory(directory);
    }

    public void ShowDirectory(string path)
    {
        var full = Path.GetFullPath(path);
        if (!System.IO.Directory.Exists(full))
            return;

        Directory = full;
        var name = Path.GetFileName(full);
        _title.Label_ = name.Length == 0 ? full : name;
        _title.TooltipText = full;
        _up.Sensitive = System.IO.Directory.GetParent(full) is not null;

        ClearRows();
        IEnumerable<string> entries;
        try
        {
            entries = System.IO.Directory.EnumerateFileSystemEntries(full);
        }
        catch (Exception ex)
        {
            AddNote(ex.Message);
            return;
        }

        var rows = entries
            .Where(entry => !Path.GetFileName(entry).StartsWith('.'))
            .Select(entry => (Path: entry, IsDirectory: System.IO.Directory.Exists(entry)))
            .OrderBy(entry => entry.IsDirectory ? 0 : 1)
            .ThenBy(entry => Path.GetFileName(entry.Path), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rows.Count == 0)
        {
            AddNote("Empty folder");
            return;
        }

        foreach (var entry in rows)
            AddEntry(entry.Path, entry.IsDirectory);
    }

    public void Select(string? filePath)
    {
        _suppress = true;
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                _list.UnselectAll();
                return;
            }

            for (var index = 0; _list.GetRowAtIndex(index) is { } row; index++)
            {
                if (_paths.TryGetValue(row, out var path) && AppPaths.SamePath(path, filePath))
                {
                    _list.SelectRow(row);
                    row.GrabFocus();
                    return;
                }
            }

            _list.UnselectAll();
        }
        finally
        {
            _suppress = false;
        }
    }

    void GoUp()
    {
        var parent = System.IO.Directory.GetParent(Directory);
        if (parent is not null)
            ShowDirectory(parent.FullName);
    }

    void Activate(ListBoxRow row)
    {
        if (_suppress || !_paths.TryGetValue(row, out var path))
            return;

        if (System.IO.Directory.Exists(path))
        {
            ShowDirectory(path);
            return;
        }

        var extension = Path.GetExtension(path);
        if (!TextExtensions.Contains(extension))
        {
            Rejected?.Invoke(path);
            return;
        }

        FileChosen?.Invoke(path);
    }

    void AddEntry(string path, bool directory)
    {
        var icon = Image.NewFromIconName(directory ? "folder-symbolic" : "text-x-generic-symbolic");
        icon.Valign = Align.Center;

        var label = Label.New(Path.GetFileName(path));
        label.Halign = Align.Start;
        label.Hexpand = true;
        label.Xalign = 0;
        label.Ellipsize = Pango.EllipsizeMode.End;

        var box = Box.New(Orientation.Horizontal, 8);
        box.MarginStart = 8;
        box.MarginEnd = 8;
        box.MarginTop = 2;
        box.MarginBottom = 2;
        box.Append(icon);
        box.Append(label);

        var row = ListBoxRow.New();
        row.Child = box;
        _paths[row] = path;
        _list.Append(row);
    }

    void AddNote(string text)
    {
        var label = Label.New(text);
        label.Halign = Align.Start;
        label.Xalign = 0;
        label.MarginStart = 12;
        label.MarginTop = 8;
        label.MarginBottom = 8;
        label.AddCssClass("dim-label");
        label.Wrap = true;

        var row = ListBoxRow.New();
        row.Child = label;
        row.Activatable = false;
        row.Selectable = false;
        _list.Append(row);
    }

    void ClearRows()
    {
        _paths.Clear();
        Widget? child;
        while ((child = _list.GetFirstChild()) is not null)
            _list.Remove(child);
    }
}
