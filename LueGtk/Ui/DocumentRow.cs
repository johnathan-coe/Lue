using Gdk;
using Gtk;
using LueGtk;

namespace LueGtk.Ui;

sealed class DocumentRow
{
    readonly DocumentView _view;
    readonly Label _label;
    readonly Picture _picture;
    readonly TextView _editor;
    bool _suppress;

    public Line Line { get; }
    public Box Root { get; }
    TextBuffer Buffer => _editor.Buffer ?? throw new InvalidOperationException("The line editor has no text buffer.");

    public DocumentRow(Line line, DocumentView view)
    {
        Line = line;
        _view = view;
        Root = Box.New(Orientation.Vertical, 0);
        Root.AddCssClass("lue-item");
        Root.Hexpand = true;

        _label = Label.New("");
        _label.Wrap = true;
        _label.WrapMode = Pango.WrapMode.WordChar;
        _label.WidthChars = 1;
        _label.Hexpand = true;
        _label.Halign = Align.Fill;
        _label.Xalign = 0;
        _label.UseMarkup = false;
        _label.Selectable = false;

        _picture = Picture.New();
        _picture.CanShrink = true;
        _picture.ContentFit = ContentFit.ScaleDown;
        _picture.Halign = Align.Start;
        _picture.Valign = Align.Start;

        _editor = TextView.New();
        _editor.WrapMode = WrapMode.WordChar;
        _editor.AcceptsTab = false;
        _editor.Hexpand = true;
        _editor.LeftMargin = 4;
        _editor.RightMargin = 4;
        _editor.TopMargin = 2;
        _editor.BottomMargin = 2;

        Root.Append(_label);
        Root.Append(_picture);
        Root.Append(_editor);

        var click = GestureClick.New();
        click.SetButton(1);
        click.OnPressed += (_, _) =>
        {
            if (!Line.Editing)
                _view.Activate(Line);
        };
        Root.AddController(click);

        var keys = EventControllerKey.New();
        keys.PropagationPhase = PropagationPhase.Capture;
        keys.OnKeyPressed += (_, args) => OnKey(args.Keyval);
        _editor.AddController(keys);
        Buffer.OnChanged += (_, _) => OnChanged();
    }

    public void Refresh()
    {
        ApplyKind(LineRenderer.KindOf(Line.Text));
        if (Line.Editing)
        {
            _label.Visible = false;
            _picture.Visible = false;
            _editor.Visible = true;
            if (CurrentText() != Line.Text)
            {
                _suppress = true;
                Buffer.SetText(Line.Text, -1);
                PlaceCursorAtEnd();
                _suppress = false;
            }
            Root.SetCursorFromName("text");
            return;
        }

        _editor.Visible = false;
        var rendered = LineRenderer.Render(Line.Text, _view.Theme, _view.Session.WorkingDirectory);
        var style = ElementStyle.For(_view.Theme, LineRenderer.KindOf(Line.Text));
        if (rendered.IsImage && TryLoad(rendered, out var paintable))
        {
            _label.Visible = false;
            _picture.Visible = true;
            _picture.Paintable = paintable!;
            _picture.AlternativeText = rendered.Text ?? Path.GetFileName(rendered.ImagePath ?? "image");
            _picture.Halign = style.Anchor == "center" ? Align.Center : Align.Start;
        }
        else
        {
            _picture.Visible = false;
            _label.Visible = true;
            _label.Label_ = rendered.Text ?? "";
            var centered = style.Justify == "center";
            _label.Justify = centered ? Justification.Center : Justification.Left;
            _label.Xalign = centered ? 0.5f : 0f;
        }

        Root.SetCursorFromName("pointer");
    }

    public void FocusEditor()
    {
        if (!Line.Editing)
            return;
        _editor.GrabFocus();
    }

    public void Fit(int width)
    {
        if (!_editor.Visible || width < 40)
            return;

        _editor.WidthRequest = width;
        _editor.Measure(Orientation.Vertical, width, out var minimum, out var natural, out _, out _);
        _editor.HeightRequest = Math.Max(minimum, natural);
    }

    public void Flush()
    {
        if (!Line.Editing)
            return;
        Line.Text = CurrentText();
    }

    void OnChanged()
    {
        if (_suppress)
            return;
        Line.Text = CurrentText();
        _view.Session.NoteEdit();
        ApplyKind(LineRenderer.KindOf(Line.Text));
    }

    bool OnKey(uint keyval)
    {
        if (keyval is Gdk.Constants.KEY_Return or Gdk.Constants.KEY_KP_Enter)
        {
            Flush();
            _view.InsertAfter(Line);
            return true;
        }

        if (keyval == Gdk.Constants.KEY_Up)
        {
            Flush();
            _view.Move(Line, -1);
            return true;
        }

        if (keyval == Gdk.Constants.KEY_Down)
        {
            Flush();
            _view.Move(Line, +1);
            return true;
        }

        if (keyval == Gdk.Constants.KEY_BackSpace && CursorOffset() == 0 && !Buffer.GetHasSelection())
        {
            Flush();
            _view.Move(Line, -1);
            return true;
        }

        return false;
    }

    string CurrentText()
    {
        var text = Buffer.Text ?? "";
        if (text.EndsWith('\n'))
            text = text[..^1];
        return text;
    }

    int CursorOffset()
    {
        var iter = new TextIter();
        Buffer.GetIterAtMark(out iter, Buffer.GetInsert());
        return iter.GetOffset();
    }

    void PlaceCursorAtEnd()
    {
        var end = new TextIter();
        Buffer.GetEndIter(out end);
        Buffer.PlaceCursor(end);
    }

    void ApplyKind(string kind)
    {
        foreach (var name in LineRenderer.Kinds)
            Root.RemoveCssClass("kind-" + name);
        Root.AddCssClass("kind-" + kind);
    }

    static bool TryLoad(RenderedContent rendered, out Gdk.Paintable? paintable)
    {
        try
        {
            if (rendered.Png is not null)
            {
                var bytes = GLib.Bytes.New(rendered.Png);
                paintable = Texture.NewFromBytes(bytes);
                return true;
            }

            if (rendered.ImagePath is not null)
            {
                paintable = Texture.NewFromFilename(rendered.ImagePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Image: " + ex.Message);
        }

        paintable = null;
        return false;
    }
}
