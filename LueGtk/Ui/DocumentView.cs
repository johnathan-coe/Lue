using Gtk;
using LueGtk;

namespace LueGtk.Ui;

sealed class DocumentView
{
    readonly List<DocumentRow> _rows = [];
    readonly Box _document;
    readonly ScrolledWindow _scroll;
    int _fittedWidth;

    public EditSession Session { get; }
    public Theme Theme { get; private set; }
    public Widget Root => _scroll;

    public DocumentView(EditSession session, Theme theme)
    {
        Session = session;
        Theme = theme;
        _document = Box.New(Orientation.Vertical, 0);
        _document.AddCssClass("lue-document");
        _document.Hexpand = true;
        _document.Vexpand = true;

        _scroll = ScrolledWindow.New();
        _scroll.AddCssClass("lue-scroll");
        _scroll.Child = _document;
        _scroll.Hexpand = true;
        _scroll.Vexpand = true;
        _scroll.PropagateNaturalWidth = false;
        _scroll.PropagateNaturalHeight = false;
        _scroll.SetPolicy(PolicyType.Never, PolicyType.Automatic);
        _scroll.OnNotify += (_, args) =>
        {
            if (args.Pspec.GetName() is "width" or "height")
                FitToViewport();
        };
        Sync();
    }

    public void SetTheme(Theme theme)
    {
        Theme = theme;
        Sync();
    }

    public void Reload() => Sync();

    public void Activate(Line line)
    {
        Flush();
        Session.Activate(line);
        Sync();
    }

    public void InsertAfter(Line line)
    {
        Flush();
        Session.InsertAfter(line);
        Sync();
    }

    public void Move(Line line, int direction)
    {
        Flush();
        Session.Move(line, direction);
        Sync();
    }

    public void Flush()
    {
        foreach (var row in _rows)
            row.Flush();
    }

    public byte[] EncodeImage(string path)
    {
        var texture = Gdk.Texture.NewFromFilename(path);
        var bytes = texture.SaveToPngBytes();
        return bytes.GetRegionSpan<byte>(UIntPtr.Zero, bytes.GetSize()).ToArray();
    }

    void Sync()
    {
        _fittedWidth = 0;
        var focus = Session.Lines.FirstOrDefault(line => line.Editing);
        foreach (var row in _rows)
            _document.Remove(row.Root);
        _rows.Clear();

        foreach (var line in Session.Lines)
        {
            var row = new DocumentRow(line, this);
            _rows.Add(row);
            _document.Append(row.Root);
            row.Refresh();
        }

        if (focus is null)
            return;

        var focused = _rows.FirstOrDefault(row => row.Line == focus);
        if (focused is null)
            return;

        GLib.Functions.TimeoutAdd(200, 50, () =>
        {
            try
            {
                FitToViewport();
                focused.FocusEditor();
                ScrollTo(focused.Root);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Focus: " + ex.Message);
            }
            return false;
        });
    }

    public void FitToViewport()
    {
        var width = _scroll.GetWidth() - 64;
        if (width < 40)
            width = 720;
        if (width == _fittedWidth)
            return;
        _fittedWidth = width;
        foreach (var row in _rows)
            row.Fit(width);
    }

    void ScrollTo(Widget widget)
    {
        if (!widget.TranslateCoordinates(_document, 0, 0, out _, out var y))
            return;

        var adjustment = _scroll.Vadjustment;
        if (adjustment is null)
            return;
        var top = y;
        var bottom = y + widget.GetHeight();
        var visibleTop = adjustment.Value;
        var visibleBottom = adjustment.Value + adjustment.PageSize;
        if (top < visibleTop)
            adjustment.Value = Math.Max(adjustment.Lower, top - 8);
        else if (bottom > visibleBottom)
            adjustment.Value = Math.Min(adjustment.Upper - adjustment.PageSize, bottom - adjustment.PageSize + 8);
    }
}
