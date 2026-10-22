using Xunit;

namespace LueGtk.Tests;

public class IniTests
{
    [Fact]
    public void ParseMergesNothingYetAndSplitsCommas()
    {
        var sections = IniFile.ParseText("""
            # comment
            [DEFAULT]
            bg = #171717
            font = Segoe UI,12

            [h1]
            font = Segoe UI Semibold,26

            [body]
            """);

        Assert.Equal("#171717", sections["DEFAULT"]["bg"].Text);
        Assert.False(sections["DEFAULT"]["bg"].IsList);
        Assert.Equal(["Segoe UI Semibold", "26"], sections["h1"]["font"].Parts);
        Assert.True(sections["h1"]["font"].IsList);
        Assert.Empty(sections["body"]);
    }

    [Fact]
    public void StyleBookAppliesDefaultToEverySectionAndFallsBack()
    {
        var book = StyleBook.FromSections(IniFile.ParseText("""
            [DEFAULT]
            bg = black
            fg = white
            [h1]
            fg = blue
            [body]
            """));

        Assert.Equal("blue", book.For("h1")["fg"].Text);
        Assert.Equal("black", book.For("h1")["bg"].Text);
        Assert.Equal("white", book.For("body")["fg"].Text);
        Assert.Equal("black", book.For("li")["bg"].Text);
        Assert.False(book.Sections.ContainsKey("DEFAULT"));
    }
}

public class ThemeTests
{
    static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "welcome.lue")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Repository root was not found.");
    }

    [Fact]
    public void BundledThemesLoadAndSwitch()
    {
        var root = RepoRoot();
        var names = new[] { "ms", "latex", "lukesmith", "lowriter" };
        var themes = names.Select(name => Theme.Load(Path.Combine(root, "themes", name))).ToList();

        Assert.Equal("26", themes[0].Appearance.For("h1")["font"].Parts[1]);
        Assert.Equal("w", themes[0].Position.For("body")["anchor"].Text);
        Assert.Equal("#171717", themes[0].Application.For("Frame")["bg"].Text);

        foreach (var from in themes)
        foreach (var to in themes)
            ThemeValidator.ValidateSwitch(from, to);
    }

    [Fact]
    public void MismatchedAttributesFailValidation()
    {
        var book = StyleBook.FromSections(IniFile.ParseText("""
            [DEFAULT]
            bg = 1
            [a]
            fg = 2
            [b]
            """));

        var error = Assert.Throws<InvalidDataException>(() => ThemeValidator.ValidateInternal(book.Sections, null));
        Assert.Contains("Discrepancy", error.Message, StringComparison.Ordinal);
        Assert.Contains("fg", error.Message, StringComparison.Ordinal);
    }
}

public class EditSessionTests
{
    [Fact]
    public void LoadSkipsBlankLinesAndSaveDropsEmptyOnes()
    {
        var directory = Directory.CreateTempSubdirectory();
        var path = Path.Combine(directory.FullName, "notes.lue");
        File.WriteAllText(path, "hello   \n\nworld\n");

        var session = new EditSession();
        session.Load(path);
        Assert.Equal(["hello", "world"], session.Lines.Select(line => line.Text).ToArray());
        Assert.False(session.Unsaved);
        Assert.Equal(directory.FullName, session.WorkingDirectory);

        session.Lines.Add(new Line());
        session.Save(path);
        Assert.Equal("hello\nworld\n", File.ReadAllText(path));
        Assert.False(session.Unsaved);
    }

    [Fact]
    public void EnterInsertsAndEmptyTailIsIgnored()
    {
        var session = new EditSession();
        var first = new Line { Text = "A", Editing = true };
        var second = new Line { Text = "B" };
        session.Lines.Add(first);
        session.Lines.Add(second);

        Assert.True(session.InsertAfter(first));
        Assert.Equal(["A", "", "B"], session.Lines.Select(line => line.Text).ToArray());
        Assert.True(session.Lines[1].Editing);
        Assert.False(first.Editing);
        Assert.True(session.Unsaved);

        var tail = new EditSession();
        var empty = new Line { Editing = true };
        tail.Lines.Add(empty);
        Assert.False(tail.InsertAfter(empty));
        Assert.Single(tail.Lines);
        Assert.False(tail.Unsaved);
    }

    [Fact]
    public void MovingAwayFromAnEmptyLineRemovesIt()
    {
        var session = new EditSession();
        var first = new Line { Text = "A" };
        var blank = new Line { Text = "", Editing = true };
        var third = new Line { Text = "B" };
        session.Lines.Add(first);
        session.Lines.Add(blank);
        session.Lines.Add(third);

        session.Move(blank, -1);
        Assert.Equal(["A", "B"], session.Lines.Select(line => line.Text).ToArray());
        Assert.True(first.Editing);
        Assert.False(session.Unsaved);
    }

    [Fact]
    public void ArrowPastTheEndAppendsALine()
    {
        var session = new EditSession();
        var only = new Line { Text = "A", Editing = true };
        session.Lines.Add(only);

        session.Move(only, +1);
        Assert.Equal(["A", ""], session.Lines.Select(line => line.Text).ToArray());
        Assert.True(session.Lines[1].Editing);
        Assert.True(session.Unsaved);
    }

    [Fact]
    public void MovingUpFromTheFirstLineDoesNothing()
    {
        var session = new EditSession();
        var only = new Line { Text = "A", Editing = true };
        session.Lines.Add(only);
        session.Move(only, -1);
        Assert.Single(session.Lines);
        Assert.True(only.Editing);
        Assert.False(session.Unsaved);
    }

    [Fact]
    public void ActivateRemovesEmptyLinesAndEditsTheTarget()
    {
        var session = new EditSession();
        var first = new Line { Text = "A", Editing = true };
        var blank = new Line();
        var third = new Line { Text = "B" };
        session.Lines.Add(first);
        session.Lines.Add(blank);
        session.Lines.Add(third);

        session.Activate(third);
        Assert.Equal(["A", "B"], session.Lines.Select(line => line.Text).ToArray());
        Assert.True(third.Editing);
        Assert.False(first.Editing);
        Assert.False(session.Unsaved);
    }

    [Fact]
    public void LaunchDirectoryStaysPutWhenNoFileIsGiven()
    {
        var launch = Directory.CreateTempSubdirectory();
        var elsewhere = Directory.CreateTempSubdirectory();
        var welcome = Path.Combine(elsewhere.FullName, "welcome.lue");
        File.WriteAllText(welcome, "# Hello\n");

        Assert.Equal(launch.FullName, AppPaths.ResolveLaunchDirectory(null, launch.FullName));
        Assert.Equal(launch.FullName, AppPaths.ResolveLaunchDirectory("missing-owd", launch.FullName));
        Assert.Equal(elsewhere.FullName, AppPaths.ResolveLaunchDirectory(elsewhere.FullName, launch.FullName));

        var session = new EditSession();
        StartupDocument.OpenInitial(session, null, welcome);
        Assert.Equal(
            launch.FullName,
            AppPaths.SidebarDirectoryForLaunch(null, launch.FullName, session.WorkingDirectory));
        Assert.Equal(elsewhere.FullName, session.WorkingDirectory);

        var notes = Path.Combine(launch.FullName, "notes.lue");
        File.WriteAllText(notes, "body\n");
        var opened = new EditSession();
        StartupDocument.OpenInitial(opened, notes, welcome);
        Assert.Equal(
            launch.FullName,
            AppPaths.SidebarDirectoryForLaunch(notes, launch.FullName, opened.WorkingDirectory));
    }

    [Fact]
    public void StartupOpensWelcomeRenderedAndOtherFilesInEditMode()
    {
        var directory = Directory.CreateTempSubdirectory();
        var welcome = Path.Combine(directory.FullName, "welcome.lue");
        File.WriteAllText(welcome, "# Hello\n\nBody\n");
        var other = Path.Combine(directory.FullName, "other.lue");
        File.WriteAllText(other, "- item\n");
        var missing = Path.Combine(directory.FullName, "missing.lue");

        var welcomeSession = new EditSession();
        StartupDocument.OpenInitial(welcomeSession, null, welcome);
        Assert.Equal(["# Hello", "Body"], welcomeSession.Lines.Select(line => line.Text).ToArray());
        Assert.DoesNotContain(welcomeSession.Lines, line => line.Editing);

        var otherSession = new EditSession();
        StartupDocument.OpenInitial(otherSession, other, welcome);
        Assert.True(otherSession.Lines[0].Editing);
        Assert.Equal("- item", otherSession.Lines[0].Text);

        var missingSession = new EditSession();
        StartupDocument.OpenInitial(missingSession, missing, welcome);
        Assert.Equal(missing, missingSession.FilePath);
        Assert.Single(missingSession.Lines);
        Assert.True(missingSession.Lines[0].Editing);
        Assert.Equal("", missingSession.Lines[0].Text);
    }

    [Fact]
    public void EveryEditRaisesChangedAndUntitledNamesSkipExistingFiles()
    {
        var directory = Directory.CreateTempSubdirectory();
        File.WriteAllText(Path.Combine(directory.FullName, "untitled.lue"), "x\n");
        Assert.Equal(
            Path.Combine(directory.FullName, "untitled-2.lue"),
            DocumentFiles.NextUntitled(directory.FullName));

        var session = new EditSession();
        var count = 0;
        session.Changed += () => count++;
        session.Lines.Add(new Line { Text = "a", Editing = true });
        session.NoteEdit();
        session.NoteEdit();
        Assert.Equal(2, count);
        Assert.True(session.Unsaved);
    }

    [Fact]
    public void SidebarKeepsLueFilesAndFoldersThatContainThem()
    {
        var root = Directory.CreateTempSubdirectory();
        Directory.CreateDirectory(Path.Combine(root.FullName, "empty"));
        var code = Path.Combine(root.FullName, "code");
        Directory.CreateDirectory(code);
        File.WriteAllText(Path.Combine(code, "main.py"), "print(1)\n");
        File.WriteAllText(Path.Combine(root.FullName, "readme.md"), "hi\n");
        File.WriteAllText(Path.Combine(root.FullName, "notes.lue"), "# Notes\n");
        File.WriteAllText(Path.Combine(root.FullName, "Notes.LUE"), "# Also\n");

        var chapters = Path.Combine(root.FullName, "chapters");
        Directory.CreateDirectory(chapters);
        File.WriteAllText(Path.Combine(chapters, "skip.txt"), "no\n");
        File.WriteAllText(Path.Combine(chapters, "one.lue"), "one\n");

        var nested = Path.Combine(root.FullName, "nested");
        Directory.CreateDirectory(Path.Combine(nested, "deep"));
        File.WriteAllText(Path.Combine(nested, "deep", "two.lue"), "two\n");
        Directory.CreateDirectory(Path.Combine(nested, "other"));
        File.WriteAllText(Path.Combine(nested, "other", "nope.md"), "no\n");

        var names = LueFolderListing.List(root.FullName).Select(entry => Path.GetFileName(entry.Path)).ToArray();
        Assert.Equal(["chapters", "nested", "Notes.LUE", "notes.lue"], names);

        var nestedNames = LueFolderListing.List(nested).Select(entry => Path.GetFileName(entry.Path)).ToArray();
        Assert.Equal(["deep"], nestedNames);
        Assert.True(LueFolderListing.List(chapters).Single().IsDirectory == false);
    }
}

public class RenderTests
{
    [Fact]
    public void ClassifiesLinesTheSameWayAsThePythonEditor()
    {
        Assert.Equal("h1", LineRenderer.KindOf("# About"));
        Assert.Equal("h2", LineRenderer.KindOf("## Project Goals"));
        Assert.Equal("h3", LineRenderer.KindOf("### TeX Math"));
        Assert.Equal("li", LineRenderer.KindOf("- Just"));
        Assert.Equal("img", LineRenderer.KindOf("^logo.png"));
        Assert.Equal("tex", LineRenderer.KindOf(@"$ x = \frac{-b}{2a}"));
        Assert.Equal("body", LineRenderer.KindOf("Lue is an editor"));
        Assert.Equal("About", LineRenderer.StripMarkers("## About"));
        Assert.Equal("• Just", LineRenderer.Render("- Just", ".").Text);
    }

    [Fact]
    public void MissingImageBecomesAnErrorString()
    {
        var rendered = LineRenderer.Render("^missing.png", Path.GetTempPath());
        Assert.Equal("Image 'missing.png' not found!", rendered.Text);
        Assert.False(rendered.IsImage);
    }

    [Fact]
    public void TexDocumentCarriesColorsAndFormula()
    {
        var document = TexRenderer.BuildDocument(@"\frac{1}{2}", "#171717", "#e6e6e6");
        Assert.Contains("171717", document, StringComparison.Ordinal);
        Assert.Contains("e6e6e6", document, StringComparison.Ordinal);
        Assert.Contains(@"\frac{1}{2}", document, StringComparison.Ordinal);
        Assert.Contains("preview", document, StringComparison.Ordinal);
    }
}
