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
    readonly Overlay _imageSlot;
    readonly Box _imageFrame;
    readonly TextView _editor;
    readonly Overlay _overlay;
    string? _presentedSource;
    bool _presentedEditing;
    bool _showingImage;
    string? _kind;
    bool _suppress;

    public Line Line { get; }
    public Widget Root { get; }
    TextBuffer Buffer => _editor.Buffer ?? throw new InvalidOperationException("The line editor has no text buffer.");

    public DocumentRow(Line line, DocumentView view)
    {
        Line = line;
        _view = view;

        _label = Label.New("");
        _label.Wrap = true;
        _label.WrapMode = Pango.WrapMode.WordChar;
        _label.WidthChars = 1;
        _label.Hexpand = true;
        _label.Halign = Align.Fill;
        _label.Valign = Align.Start;
        _label.Xalign = 0;
        _label.UseMarkup = false;
        _label.Selectable = false;

        _picture = Picture.New();
        _picture.CanShrink = true;
        _picture.Hexpand = false;
        _picture.Vexpand = false;
        _picture.ContentFit = ContentFit.ScaleDown;
        _picture.Halign = Align.Fill;
        _picture.Valign = Align.Fill;

        // Picture reports a height from the full column width. The frame's size
        // request is the real image size, and the picture is only painted on top.
        _imageFrame = Box.New(Orientation.Horizontal, 0);
        _imageFrame.Halign = Align.Start;
        _imageFrame.Valign = Align.Start;
        _imageSlot = Overlay.New();
        _imageSlot.Child = _imageFrame;
        _imageSlot.Hexpand = false;
        _imageSlot.Vexpand = false;
        _imageSlot.Halign = Align.Start;
        _imageSlot.Valign = Align.Start;
        _imageSlot.AddOverlay(_picture);
        _imageSlot.Visible = false;

        _editor = TextView.New();
        _editor.WrapMode = WrapMode.WordChar;
        _editor.AcceptsTab = false;
        _editor.Hexpand = true;
        _editor.Vexpand = true;
        _editor.Halign = Align.Fill;
        _editor.Valign = Align.Fill;
        _editor.LeftMargin = 0;
        _editor.RightMargin = 0;
        _editor.TopMargin = 0;
        _editor.BottomMargin = 0;
        _editor.AddCssClass("lue-line");

        var page = Box.New(Orientation.Vertical, 0);
        page.Hexpand = true;
        page.Append(_label);
        page.Append(_imageSlot);

        // The rendered line stays in the layout. The editor is drawn on top of
        // it and is not measured, so showing the caret does not collapse or
        // rebuild the row.
        _overlay = Overlay.New();
        _overlay.Hexpand = true;
        _overlay.Child = page;
        _overlay.AddOverlay(_editor);
        _overlay.SetMeasureOverlay(_editor, false);
        Root = _overlay;

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
        if (_presentedSource == Line.Text && _presentedEditing == Line.Editing)
            return;

        var placeCursor = Line.Editing && !_presentedEditing;
        _presentedSource = Line.Text;
        _presentedEditing = Line.Editing;
        ApplyKind(LineRenderer.KindOf(Line.Text));

        if (Line.Editing)
        {
            ShowEditor(placeCursor);
            return;
        }

        var colors = WidgetColors(_label);
        var rendered = LineRenderer.Render(Line.Text, _view.Session.WorkingDirectory, colors.Background, colors.Foreground);
        if (rendered.IsImage && TryLoad(rendered, out var paintable))
        {
            _showingImage = true;
            _label.Visible = false;
            _picture.Paintable = paintable!;
            _picture.AlternativeText = rendered.Text ?? Path.GetFileName(rendered.ImagePath ?? "image");
            _picture.Visible = true;
            _imageSlot.Visible = true;
            FitPicture(int.MaxValue);
            HideEditor();
            Root.SetCursorFromName("pointer");
            return;
        }

        _showingImage = false;
        _imageSlot.Visible = false;
        _imageFrame.WidthRequest = -1;
        _imageFrame.HeightRequest = -1;
        _label.Visible = true;
        _label.Opacity = 1;
        _label.Label_ = rendered.Text ?? "";
        _label.Justify = Justification.Left;
        _label.Xalign = 0;
        HideEditor();
        Root.SetCursorFromName("pointer");
    }

    void ShowEditor(bool placeCursor)
    {
        _showingImage = false;
        _imageSlot.Visible = false;
        _imageFrame.WidthRequest = -1;
        _imageFrame.HeightRequest = -1;

        // The invisible label keeps the rendered line's height while the editor
        // is drawn on top, including before the editor can be measured.
        var colors = WidgetColors(_label);
        var rendered = LineRenderer.Render(Line.Text, _view.Session.WorkingDirectory, colors.Background, colors.Foreground);
        _label.Label_ = rendered.IsImage ? Line.Text : rendered.Text ?? "";
        _label.Visible = true;
        _label.Opacity = 0;
        _editor.Visible = true;
        _editor.Opacity = 1;
        _editor.Editable = true;
        _editor.CursorVisible = true;
        _editor.CanFocus = true;
        _editor.CanTarget = true;
        if (CurrentText() != Line.Text)
        {
            _suppress = true;
            Buffer.SetText(Line.Text, -1);
            _suppress = false;
            placeCursor = true;
        }

        if (placeCursor)
            PlaceCursorAtEnd();
        Root.SetCursorFromName("text");
    }

    void HideEditor()
    {
        if (CurrentText() != Line.Text)
        {
            _suppress = true;
            Buffer.SetText(Line.Text, -1);
            _suppress = false;
        }

        _editor.Editable = false;
        _editor.CursorVisible = false;
        _editor.CanFocus = false;
        _editor.CanTarget = false;
        _editor.Opacity = 0;
        _editor.Visible = true;
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
        if (width < 40)
            return;

        if (_showingImage)
        {
            if (_overlay.HeightRequest != -1)
                _overlay.HeightRequest = -1;
            FitPicture(width);
            return;
        }

        // Hold the taller of the rendered line and the editor so the swap
        // does not resize the row. A measurement of 0 means the widget is not
        // on a display yet; leaving the request unset keeps the label's height.
        _label.Measure(Orientation.Vertical, width, out var labelMin, out var labelNat, out _, out _);
        _editor.Measure(Orientation.Vertical, width, out var editorMin, out var editorNat, out _, out _);
        var height = Math.Max(Math.Max(labelMin, labelNat), Math.Max(editorMin, editorNat));
        if (height > 0 && _overlay.HeightRequest != height)
            _overlay.HeightRequest = height;
    }

    void FitPicture(int width)
    {
        var paintable = _picture.Paintable;
        if (paintable is null || width < 1)
            return;

        var intrinsicWidth = paintable.GetIntrinsicWidth();
        var intrinsicHeight = paintable.GetIntrinsicHeight();
        if (intrinsicWidth <= 0 || intrinsicHeight <= 0)
            return;

        var displayWidth = Math.Min(width, intrinsicWidth);
        var displayHeight = Math.Max(1, (int)Math.Round(intrinsicHeight * (displayWidth / (double)intrinsicWidth)));
        _imageFrame.WidthRequest = displayWidth;
        _imageFrame.HeightRequest = displayHeight;
    }

    public void Flush()
    {
        if (!Line.Editing)
            return;
        var text = CurrentText();
        if (text == Line.Text)
            return;
        Line.Text = text;
        _view.Session.NoteEdit();
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
        if (Line.Editing)
            _editor.GrabFocus();
        _view.Refit();

        if (_colorsReady || Line.Editing || LineRenderer.KindOf(Line.Text) != LineRenderer.Tex)
            return;
        _presentedSource = null;
        Refresh();
    }

    void ApplyKind(string kind)
    {
        if (kind == _kind)
            return;
        _kind = kind;

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
