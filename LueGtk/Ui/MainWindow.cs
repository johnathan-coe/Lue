using Gtk;
using LueGtk;

namespace LueGtk.Ui;

sealed class MainWindow
{
    readonly Application _application;
    readonly ApplicationWindow _window;
    readonly EditSession _session;
    readonly DocumentView _view;
    readonly FolderSidebar _sidebar;
    readonly Revealer _sidebarPane;
    readonly ToggleButton _sidebarToggle;
    readonly Label _status;
    bool _sidebarOpen = true;
    bool _saving;

    public MainWindow(Application application, string? argument)
    {
        _application = application;
        _session = new EditSession();
        _session.Changed += Autosave;

        StartupDocument.OpenInitial(_session, argument, AppPaths.Welcome);
        _view = new DocumentView(_session);

        _window = ApplicationWindow.New(application);
        _window.Application = application;
        _window.Title = "Lue";
        _window.SetDefaultSize(1080, 720);
        _window.OnCloseRequest += (_, _) => OnClose();

        _sidebar = new FolderSidebar(_session.WorkingDirectory);
        _sidebar.FileChosen += path => _ = OpenFromSidebarAsync(path);
        _sidebar.Select(_session.FilePath);

        _sidebarPane = Revealer.New();
        _sidebarPane.TransitionType = RevealerTransitionType.SlideRight;
        _sidebarPane.RevealChild = true;
        _sidebarPane.Child = _sidebar.Root;

        _sidebarToggle = ToggleButton.New();
        _sidebarToggle.IconName = "sidebar-show-symbolic";
        _sidebarToggle.Active = true;
        _sidebarToggle.TooltipText = "Folder sidebar (F9)";
        _sidebarToggle.AddCssClass("flat");
        _sidebarToggle.Valign = Align.Center;
        _sidebarToggle.OnToggled += (_, _) => SetSidebar(_sidebarToggle.Active);

        var menu = BuildMenu();
        menu.Hexpand = true;

        var menuRow = Box.New(Orientation.Horizontal, 0);
        menuRow.Append(_sidebarToggle);
        menuRow.Append(menu);

        var body = Box.New(Orientation.Horizontal, 0);
        body.Hexpand = true;
        body.Vexpand = true;
        body.Append(_sidebarPane);
        body.Append(_view.Root);

        var root = Box.New(Orientation.Vertical, 0);
        root.Append(menuRow);
        root.Append(body);

        var separator = Separator.New(Orientation.Horizontal);
        root.Append(separator);

        _status = Label.New(SavedText(ExistingSaveTime() ?? DateTime.Now));
        _status.AddCssClass("dim-label");
        _status.Halign = Align.Fill;
        _status.Xalign = 0;
        _status.MarginTop = 6;
        _status.MarginBottom = 6;
        _status.MarginStart = 12;
        _status.MarginEnd = 12;
        root.Append(_status);

        _window.Child = root;
        UpdateTitle();
        InstallActions();
    }

    public void Present() => _window.Present();

    Widget BuildMenu()
    {
        var file = Gio.Menu.New();
        file.Append("New", "app.new");
        file.Append("Open", "app.open");

        var view = Gio.Menu.New();
        view.Append("Folder Sidebar", "app.sidebar");

        var bar = Gio.Menu.New();
        bar.AppendSubmenu("File", file);
        bar.AppendSubmenu("View", view);
        return PopoverMenuBar.NewFromModel(bar);
    }

    void InstallActions()
    {
        Add("new", NewDocument, "<Primary>n");
        Add("open", () => _ = OpenAsync(), "<Primary>o");
        Add("quit", () => _window.Close(), "<Primary>q");
        Add("sidebar", ToggleSidebar, "F9");
    }

    void Add(string name, Action callback, string? accelerator)
    {
        var action = Gio.SimpleAction.New(name, null!);
        action.OnActivate += (_, _) => callback();
        _application.AddAction(action);
        if (accelerator is not null)
            _application.SetAccelsForAction("app." + name, [accelerator]);
    }

    bool OnClose()
    {
        try
        {
            Persist();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }

        return false;
    }

    void Persist()
    {
        _view.Flush();
        if (!_session.Unsaved || string.IsNullOrEmpty(_session.FilePath))
            return;
        _session.Save(_session.FilePath);
    }

    void NewDocument()
    {
        Persist();
        var directory = _sidebar.Directory;
        _session.NewDocument();
        var path = DocumentFiles.NextUntitled(directory);
        _session.Save(path);
        _view.Reload();
        UpdateTitle();
        ShowSaved(DateTime.Now);
        SyncSidebar();
    }

    async Task OpenAsync()
    {
        var dialog = FileDialog.New();
        dialog.Title = "Open";
        dialog.Modal = true;
        dialog.Filters = FileFilters();
        try
        {
            var file = await dialog.OpenAsync(_window);
            var path = file?.GetPath();
            if (string.IsNullOrEmpty(path))
                return;
            Persist();
            LoadDocument(path);
        }
        catch (GLib.GException ex) when (IsCancel(ex))
        {
        }
        catch (Exception ex)
        {
            await ErrorAsync("Could not open", ex.Message);
        }
    }

    void Autosave()
    {
        if (_saving)
            return;

        _saving = true;
        try
        {
            _view.Flush();
            var path = _session.FilePath;
            var created = string.IsNullOrEmpty(path) || !File.Exists(path);
            if (string.IsNullOrEmpty(path))
                path = DocumentFiles.NextUntitled(_sidebar.Directory);

            _session.Save(path!);
            ShowSaved(DateTime.Now);
            if (created)
            {
                UpdateTitle();
                SyncSidebar();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            SetStatus("Could not autosave");
        }
        finally
        {
            _saving = false;
        }
    }

    async Task ErrorAsync(string message, string detail)
    {
        var dialog = AlertDialog.NewWithProperties([]);
        dialog.Message = message;
        dialog.Detail = detail;
        dialog.Buttons = ["OK"];
        dialog.DefaultButton = 0;
        dialog.CancelButton = 0;
        dialog.Modal = true;
        try { await dialog.ChooseAsync(_window); }
        catch (GLib.GException) { }
    }

    Gio.ListStore FileFilters()
    {
        var store = Gio.ListStore.New(FileFilter.GetGType());
        store.Append(Filter("Lue Files", "*.lue"));
        store.Append(Filter("Markdown files", "*.md"));
        var all = FileFilter.New();
        all.Name = "All files";
        all.AddPattern("*");
        all.AddPattern("*.*");
        store.Append(all);
        return store;
    }

    static FileFilter Filter(string name, string pattern)
    {
        var filter = FileFilter.New();
        filter.Name = name;
        filter.AddPattern(pattern);
        return filter;
    }

    async Task OpenFromSidebarAsync(string path)
    {
        if (!string.IsNullOrEmpty(_session.FilePath) && AppPaths.SamePath(path, _session.FilePath))
            return;

        try
        {
            Persist();
            LoadDocument(path);
        }
        catch (Exception ex)
        {
            await ErrorAsync("Could not open", ex.Message);
            _sidebar.Select(_session.FilePath);
        }
    }

    void LoadDocument(string path)
    {
        _session.Load(path);
        if (_session.Lines.Count == 0)
            _session.Lines.Add(new Line { Editing = true });
        else
            _session.EditFirst();
        _view.Reload();
        UpdateTitle();
        ShowSaved(ExistingSaveTime() ?? DateTime.Now);
        SyncSidebar();
    }

    void SyncSidebar()
    {
        var directory = string.IsNullOrEmpty(_session.FilePath)
            ? _session.WorkingDirectory
            : Path.GetDirectoryName(Path.GetFullPath(_session.FilePath)) ?? _session.WorkingDirectory;
        _sidebar.ShowDirectory(directory);
        _sidebar.Select(_session.FilePath);
    }

    void ToggleSidebar() => SetSidebar(!_sidebarOpen);

    void SetSidebar(bool open)
    {
        _sidebarOpen = open;
        _sidebarPane.RevealChild = open;
        if (_sidebarToggle.Active != open)
            _sidebarToggle.Active = open;
    }

    void UpdateTitle()
    {
        _window.Title = string.IsNullOrEmpty(_session.FilePath)
            ? "Lue"
            : "Lue — " + Path.GetFileName(_session.FilePath);
    }

    void ShowSaved(DateTime when) => SetStatus(SavedText(when));

    DateTime? ExistingSaveTime()
    {
        var path = _session.FilePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;
        return File.GetLastWriteTime(path);
    }

    static string SavedText(DateTime when) => $"Saved at {when:HH:mm:ss}";

    void SetStatus(string text) => _status.Label_ = text;

    static bool IsCancel(GLib.GException exception)
    {
        var message = exception.Message;
        return message.Contains("cancel", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("dismiss", StringComparison.OrdinalIgnoreCase);
    }
}
