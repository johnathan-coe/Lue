using Gtk;
using LueGtk;

namespace LueGtk.Ui;

sealed class MainWindow
{
    readonly Application _application;
    readonly ApplicationWindow _window;
    readonly EditSession _session;
    readonly DocumentView _view;
    readonly Label _status;
    Theme _theme;
    CssProvider? _css;
    bool _prompting;
    bool _allowClose;

    public MainWindow(Application application, string? argument)
    {
        _application = application;
        _session = new EditSession();
        _session.Dirtied += () => SetStatus("Unsaved changes");

        IReadOnlyCollection<string>? fonts = null;
        try { fonts = FontFamilies.Load(); }
        catch (Exception ex) { Console.Error.WriteLine("Fonts: " + ex.Message); }

        _theme = Theme.Load(AppPaths.DefaultTheme, fonts);
        StartupDocument.OpenInitial(_session, argument, AppPaths.Welcome);
        _view = new DocumentView(_session, _theme);

        _window = ApplicationWindow.New(application);
        _window.Application = application;
        _window.Title = "Lue";
        _window.SetDefaultSize(860, 720);
        _window.AddCssClass("lue-window");
        _window.OnCloseRequest += (_, _) => OnClose();

        var root = Box.New(Orientation.Vertical, 0);
        root.Append(BuildMenu());
        root.Append(_view.Root);

        var separator = Separator.New(Orientation.Horizontal);
        root.Append(separator);

        _status = Label.New("Unsaved since load!");
        _status.AddCssClass("lue-status");
        _status.Halign = Align.Fill;
        _status.Xalign = 0;
        root.Append(_status);

        _window.Child = root;
        _window.OnRealize += (_, _) =>
        {
            if (_css is null)
                ApplyCss(ThemeCss.Build(_theme));
        };
        ApplyCss(ThemeCss.Build(_theme));
        UpdateTitle();
        InstallActions();
    }

    public void Present() => _window.Present();

    Widget BuildMenu()
    {
        var file = Gio.Menu.New();
        file.Append("New", "app.new");
        file.Append("Open", "app.open");
        file.Append("Save", "app.save");
        file.Append("Save As", "app.save-as");

        var themes = Gio.Menu.New();
        foreach (var directory in AppPaths.ThemeDirectories())
        {
            var name = Path.GetFileName(directory);
            themes.Append(name, "app.theme-" + name);
        }

        var export = Gio.Menu.New();
        export.Append(HtmlExporter.Name, "app.export-html");

        var bar = Gio.Menu.New();
        bar.AppendSubmenu("File", file);
        bar.AppendSubmenu("Theme", themes);
        bar.AppendSubmenu("Export", export);
        return PopoverMenuBar.NewFromModel(bar);
    }

    void InstallActions()
    {
        Add("new", () => _ = NewAsync(), "<Primary>n");
        Add("open", () => _ = OpenAsync(), "<Primary>o");
        Add("save", () => _ = SaveAsync(), "<Primary>s");
        Add("save-as", () => _ = SaveAsAsync(), "<Primary><Shift>s");
        Add("quit", () => _window.Close(), "<Primary>q");
        Add("export-html", ExportHtml, null);
        foreach (var directory in AppPaths.ThemeDirectories())
        {
            var captured = directory;
            var name = Path.GetFileName(captured);
            Add("theme-" + name, () => SwitchTheme(captured), null);
        }
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
        if (_allowClose || !_session.Unsaved)
            return false;
        if (_prompting)
            return true;

        _prompting = true;
        _ = ConfirmCloseAsync();
        return true;
    }

    async Task ConfirmCloseAsync()
    {
        try
        {
            if (await EnsureSavedAsync())
            {
                _allowClose = true;
                _window.Close();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            await ErrorAsync("Could not close", ex.Message);
        }
        finally
        {
            _prompting = false;
        }
    }

    async Task<bool> EnsureSavedAsync()
    {
        _view.Flush();
        if (!_session.Unsaved)
            return true;

        var dialog = AlertDialog.NewWithProperties([]);
        dialog.Message = "Unsaved Changes!";
        dialog.Detail = "Save changes?";
        dialog.Buttons = ["Don't Save", "Cancel", "Save"];
        dialog.CancelButton = 1;
        dialog.DefaultButton = 2;
        dialog.Modal = true;

        var choice = await dialog.ChooseAsync(_window);
        if (choice == 0)
            return true;
        if (choice == 2)
            return await SaveAsync();
        return false;
    }

    async Task NewAsync()
    {
        if (!await EnsureSavedAsync())
            return;
        _session.NewDocument();
        _view.Reload();
        UpdateTitle();
        SetStatus("Unsaved since load!");
    }

    async Task OpenAsync()
    {
        if (!await EnsureSavedAsync())
            return;

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
            _session.Load(path);
            if (_session.Lines.Count == 0)
                _session.Lines.Add(new Line { Editing = true });
            else
                _session.EditFirst();
            _view.Reload();
            UpdateTitle();
            SetStatus("Unsaved since load!");
        }
        catch (GLib.GException ex) when (IsCancel(ex))
        {
        }
        catch (Exception ex)
        {
            await ErrorAsync("Could not open", ex.Message);
        }
    }

    async Task<bool> SaveAsync()
    {
        _view.Flush();
        if (string.IsNullOrEmpty(_session.FilePath))
            return await SaveAsAsync();

        try
        {
            SetStatus("Saving...");
            _session.Save(_session.FilePath);
            SetStatus($"Saved at {DateTime.Now:HH:mm:ss.ffffff}");
            UpdateTitle();
            _view.Reload();
            return true;
        }
        catch (Exception ex)
        {
            await ErrorAsync("Could not save", ex.Message);
            return false;
        }
    }

    async Task<bool> SaveAsAsync()
    {
        _view.Flush();
        var dialog = FileDialog.New();
        dialog.Title = "Save As";
        dialog.Modal = true;
        dialog.Filters = FileFilters();
        dialog.InitialName = string.IsNullOrEmpty(_session.FilePath)
            ? "untitled.lue"
            : Path.GetFileName(_session.FilePath);
        if (!string.IsNullOrEmpty(_session.FilePath))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_session.FilePath));
            if (!string.IsNullOrEmpty(directory))
                dialog.InitialFolder = Gio.FileHelper.NewForPath(directory);
        }

        try
        {
            var file = await dialog.SaveAsync(_window);
            var path = file?.GetPath();
            if (string.IsNullOrEmpty(path))
                return false;
            SetStatus("Saving...");
            _session.Save(path);
            SetStatus($"Saved at {DateTime.Now:HH:mm:ss.ffffff}");
            UpdateTitle();
            _view.Reload();
            return true;
        }
        catch (GLib.GException ex) when (IsCancel(ex))
        {
            return false;
        }
        catch (Exception ex)
        {
            await ErrorAsync("Could not save", ex.Message);
            return false;
        }
    }

    void ExportHtml()
    {
        try
        {
            _view.Flush();
            var path = HtmlExporter.Write(_session, _theme, _view.EncodeImage);
            SetStatus("Exported HTML to " + path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            _ = ErrorAsync("Could not export", ex.Message);
        }
    }

    void SwitchTheme(string directory)
    {
        try
        {
            IReadOnlyCollection<string>? fonts = null;
            try { fonts = FontFamilies.Load(); }
            catch (Exception) { }

            var next = Theme.Load(directory, fonts);
            ThemeValidator.ValidateSwitch(_theme, next);
            _theme = next;
            ApplyCss(ThemeCss.Build(next));
            _view.SetTheme(next);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            _ = ErrorAsync("Could not apply theme", ex.Message);
        }
    }

    void ApplyCss(string css)
    {
        var display = Gdk.Display.GetDefault();
        if (display is null)
            return;

        if (_css is not null)
            StyleContext.RemoveProviderForDisplay(display, _css);

        _css = CssProvider.New();
        _css.OnParsingError += (_, args) => Console.Error.WriteLine("CSS: " + args.Error);
        _css.LoadFromString(css);
        StyleContext.AddProviderForDisplay(display, _css, Constants.STYLE_PROVIDER_PRIORITY_APPLICATION);
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

    void UpdateTitle()
    {
        _window.Title = string.IsNullOrEmpty(_session.FilePath)
            ? "Lue"
            : "Lue — " + Path.GetFileName(_session.FilePath);
    }

    void SetStatus(string text) => _status.Label_ = text;

    static bool IsCancel(GLib.GException exception)
    {
        var message = exception.Message;
        return message.Contains("cancel", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("dismiss", StringComparison.OrdinalIgnoreCase);
    }
}
