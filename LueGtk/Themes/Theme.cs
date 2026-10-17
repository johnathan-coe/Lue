namespace LueGtk;

public sealed class Theme
{
    public string Name { get; }
    public string Directory { get; }
    public StyleBook Appearance { get; }
    public StyleBook Position { get; }
    public StyleBook Application { get; }

    Theme(string name, string directory, StyleBook appearance, StyleBook position, StyleBook application)
    {
        Name = name;
        Directory = directory;
        Appearance = appearance;
        Position = position;
        Application = application;
    }

    public static Theme Load(string directory, IReadOnlyCollection<string>? fontFamilies = null)
    {
        var appearance = StyleBook.Load(Path.Combine(directory, "elements", "appearance.ini"));
        ThemeValidator.ValidateInternal(appearance.Sections, fontFamilies);

        var position = StyleBook.Load(Path.Combine(directory, "elements", "position.ini"));
        ThemeValidator.ValidateInternal(position.Sections, fontFamilies);

        var application = StyleBook.Load(Path.Combine(directory, "application.ini"));
        ThemeValidator.ValidateInternal(application.Sections, fontFamilies);

        return new Theme(Path.GetFileName(directory), directory, appearance, position, application);
    }
}
