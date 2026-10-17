using Gdk;
using Gtk;
using LueGtk;

namespace LueGtk.Ui;

sealed class DocumentRow
{
    static readonly string[] NativeClasses = ["title-1", "title-2", "title-3", "body"];
    static bool _colorsReady;

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
        Root.OnMap += (_, _) => OnMapped();

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
        var colors = WidgetColors(_label);
        var rendered = LineRenderer.Render(Line.Text, _view.Session.WorkingDirectory, colors.Background, colors.Foreground);
        if (rendered.IsImage && TryLoad(rendered, out var paintable))
        {
            _label.Visible = false;
            _picture.Visible = true;
            _picture.Paintable = paintable!;
            _picture.AlternativeText = rendered.Text ?? Path.GetFileName(rendered.ImagePath ?? "image");
            _picture.Halign = Align.Start;
        }
        else
        {
            _picture.Visible = false;
            _label.Visible = true;
            _label.Label_ = rendered.Text ?? "";
            _label.Justify = Justification.Left;
            _label.Xalign = 0;
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
        width -= Root.MarginStart;
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

    void OnMapped()
    {
        if (_colorsReady || Line.Editing || LineRenderer.KindOf(Line.Text) != LineRenderer.Tex)
            return;
        Refresh();
    }

    void ApplyKind(string kind)
    {
        foreach (var name in NativeClasses)
        {
            _label.RemoveCssClass(name);
            _editor.RemoveCssClass(name);
        }

        var native = kind switch
        {
            LineRenderer.Heading1 => "title-1",
            LineRenderer.Heading2 => "title-2",
            LineRenderer.Heading3 => "title-3",
            _ => "body"
        };
        _label.AddCssClass(native);
        _editor.AddCssClass(native);
        Root.MarginStart = kind == LineRenderer.Bullet ? 16 : 0;
    }

    static (string Background, string Foreground) WidgetColors(Widget widget)
    {
        var context = widget.GetStyleContext();
        var background = NamedColor(context, "theme_bg_color") ?? NamedColor(context, "window_bg_color") ?? "";
        var foreground = NamedColor(context, "theme_fg_color") ?? NamedColor(context, "window_fg_color") ?? "";
        if (foreground.Length == 0)
        {
            widget.GetColor(out var color);
            foreground = ToHex(color);
        }
        if (background.Length > 0 && foreground.Length > 0)
            _colorsReady = true;
        return (background, foreground);
    }

    static string? NamedColor(StyleContext context, string name)
    {
        return context.LookupColor(name, out var color) ? ToHex(color) : null;
    }

    static string ToHex(RGBA color)
    {
        static int Channel(float value) => (int)Math.Clamp(Math.Round(value * 255), 0, 255);
        return $"#{Channel(color.Red):X2}{Channel(color.Green):X2}{Channel(color.Blue):X2}";
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
