using Gio;
using Gtk;
using LueGtk;
using LueGtk.Ui;

var launchDirectory = AppPaths.ResolveLaunchDirectory(
    Environment.GetEnvironmentVariable("OWD"),
    Directory.GetCurrentDirectory());

AppStyle.RegisterFonts();
Gtk.Module.Initialize();

var application = Gtk.Application.New("org.lue.Editor", Gio.ApplicationFlags.NonUnique);
var argument = args.Length > 0 ? args[0] : null;

application.OnActivate += (sender, _) =>
{
    AppStyle.Apply();
    var window = new MainWindow((Gtk.Application)sender, argument, launchDirectory);
    window.Present();
};

return application.RunWithSynchronizationContext(null);
