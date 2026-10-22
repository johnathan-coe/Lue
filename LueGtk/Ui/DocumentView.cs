using Gtk;
using LueGtk;

namespace LueGtk.Ui;

sealed class DocumentView
{
    readonly List<DocumentRow> _rows = [];
    readonly Box _document;
    readonly ScrolledWindow _scroll;
    Widget? _pendingScroll;
    int _fittedWidth;
    bool _fitting;

    public EditSession Session { get; }
    public Widget Root => _scroll;

    public DocumentView(EditSession session)
    {
        Session = session;
        _document = Box.New(Orientation.Vertical, 6);
        _document.MarginTop = 12;
        _document.MarginBottom = 12;
        _document.MarginStart = 12;
        _document.MarginEnd = 12;
        _document.Hexpand = true;
        _document.Vexpand = true;

        _scroll = ScrolledWindow.New();
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

    void Sync()
    {
        var pool = new Dictionary<Line, DocumentRow>(_rows.Count);
        foreach (var row in _rows)
            pool[row.Line] = row;

        var next = new List<DocumentRow>(Session.Lines.Count);
        foreach (var line in Session.Lines)
        {
            if (!pool.Remove(line, out var row))
                row = new DocumentRow(line, this);
            next.Add(row);
        }

        var sameOrder = next.Count == _rows.Count;
        if (sameOrder)
        {
            for (var i = 0; i < next.Count; i++)
            {
                if (!ReferenceEquals(next[i], _rows[i]))
                {
                    sameOrder = false;
                    break;
                }
            }
        }

        if (!sameOrder)
        {
            Widget? previous = null;
            foreach (var row in next)
            {
                if (row.Root.GetParent() is null)
                    _document.InsertChildAfter(row.Root, previous);
                else
                    _document.ReorderChildAfter(row.Root, previous);
                previous = row.Root;
            }

            foreach (var removed in pool.Values)
                _document.Remove(removed.Root);
            _rows.Clear();
            _rows.AddRange(next);
        }

        foreach (var row in _rows)
            row.Refresh();

        var focused = _rows.FirstOrDefault(row => row.Line.Editing);
        _pendingScroll = focused?.Root;
        FitLines(force: true);
        focused?.FocusEditor();
        ScrollPending();
    }

    public void Refit()
    {
        FitLines(force: true);
        ScrollPending();
    }

    public void FitToViewport() => FitLines(force: false);

    void FitLines(bool force)
    {
        if (_fitting)
            return;
        var width = _scroll.GetWidth() - _document.MarginStart - _document.MarginEnd - 16;
        if (width < 40)
            width = 720;
        if (!force && width == _fittedWidth)
            return;

        _fitting = true;
        try
        {
            _fittedWidth = width;
            foreach (var row in _rows)
                row.Fit(width);
        }
        finally
        {
            _fitting = false;
        }
    }

    void ScrollPending()
    {
        if (_pendingScroll is null || _pendingScroll.GetHeight() <= 0)
            return;
        ScrollTo(_pendingScroll);
        _pendingScroll = null;
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
