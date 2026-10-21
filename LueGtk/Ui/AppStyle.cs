using System.Reflection;
using System.Runtime.InteropServices;

namespace LueGtk.Ui;

/// <summary>
/// Selects GTK's dark theme and the bundled Inter face.
/// Colors stay with the toolkit; only the preferred variant and the font are set.
/// </summary>
static class AppStyle
{
    public const string FontDescription = "Inter 11";

    public static void RegisterFonts()
    {
        var directory = FontDirectory();
        if (directory is null)
        {
            Console.Error.WriteLine("Fonts: bundled Inter files were not found.");
            return;
        }

        try
        {
            FontConfig.AddDirectory(directory);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Fonts: " + ex.Message);
        }
    }

    public static void Apply()
    {
        var settings = Gtk.Settings.GetDefault();
        if (settings is null)
            return;

        settings.GtkApplicationPreferDarkTheme = true;
        settings.GtkFontName = FontDescription;
    }

    static string? FontDirectory()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                foreach (var candidate in new[]
                {
                    Path.Combine(dir.FullName, "Fonts"),
                    Path.Combine(dir.FullName, "LueGtk", "Fonts"),
                    Path.Combine(dir.FullName, "share", "lue", "Fonts")
                })
                {
                    if (File.Exists(Path.Combine(candidate, "Inter-Regular.ttf")))
                        return candidate;
                }

                dir = dir.Parent;
            }
        }

        return null;
    }

    static class FontConfig
    {
        static FontConfig()
        {
            NativeLibrary.SetDllImportResolver(typeof(AppStyle).Assembly, Resolve);
        }

        public static void AddDirectory(string directory)
        {
            var path = Encoding.UTF8.GetBytes(directory + "\0");
            if (FcConfigAppFontAddDir(IntPtr.Zero, path) == 0)
                Console.Error.WriteLine("Fonts: fontconfig rejected " + directory);
        }

        [DllImport("fontconfig", EntryPoint = "FcConfigAppFontAddDir")]
        static extern int FcConfigAppFontAddDir(IntPtr config, byte[] directory);

        static IntPtr Resolve(string library, Assembly assembly, DllImportSearchPath? search)
        {
            if (library != "fontconfig")
                return IntPtr.Zero;

            string[] names = OperatingSystem.IsWindows()
                ? ["libfontconfig-1.dll", "fontconfig-1.dll", "fontconfig.dll"]
                : ["libfontconfig.so.1", "libfontconfig.so"];

            foreach (var name in names)
            {
                if (NativeLibrary.TryLoad(name, assembly, search, out var handle))
                    return handle;
            }

            return IntPtr.Zero;
        }
    }
}
