# Lue GTK

Native GTK 4 port of the Lue editor. The interface is built in C# with [Gir.Core](https://gircore.github.io/) (`GirCore.Gtk-4.0`). There is no XAML and no UI markup: windows, rows, and menus are created in code. The same project runs on Linux and Windows.

Lue edits a line-oriented document. A line renders as a heading, body text, bullet, image, or TeX formula, and a click (or the arrow keys) turns that line back into an entry.

| Prefix | Result |
| --- | --- |
| `#`, `##`, `###` | Headings |
| `-` | Bullet |
| `^path` | Image, resolved next to the document |
| `$formula` | TeX math, when a LaTeX install is available |
| anything else | Body text |

The window uses GTK’s dark theme and the Inter typeface. There is no theme menu and no custom color stylesheet: headings use GTK’s `title-1`, `title-2`, and `title-3` styles, and the rest of the text uses the theme’s body style. Inter is bundled with the app under the SIL Open Font License so Linux and Windows share the same face. A folder sidebar lists .lue files in the directory the app was launched from. Folders appear only when they contain a .lue file. Choose a folder to open it, or a file to edit it. Opening a document switches the sidebar to that document’s folder. The sidebar button and View → Folder Sidebar hide it; F9 does the same. Edits are written to the open `.lue` file as they happen, so there is no Save command and closing does not ask about unsaved changes. File → New creates the next `untitled.lue` in the folder you are browsing. Ctrl+N starts a new document and Ctrl+O opens one.

## Linux

Install the .NET 8 SDK and the GTK 4 runtime (4.10 or newer):

```bash
sudo apt install dotnet-sdk-8.0 libgtk-4-1
dotnet run --project LueGtk/LueGtk.csproj
```

Open a document by passing its path:

```bash
dotnet run --project LueGtk/LueGtk.csproj -- notes.lue
```

TeX lines need `pdflatex` and either `pdftocairo` (poppler-utils) or ImageMagick. Without them, a formula line shows `TeXMath Rendering Error!`, which is the same fallback as the Python app.

## Windows

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download).
2. Install [MSYS2](https://www.msys2.org/), then in the MSYS2 UCRT64 shell install GTK 4:

   ```bash
   pacman -Syu
   pacman -S mingw-w64-ucrt-x86_64-gtk4
   ```

3. Add `C:\msys64\ucrt64\bin` to the front of `PATH` if you installed the UCRT64 toolchain, or `C:\msys64\mingw64\bin` for the MINGW64 toolchain. Gir.Core loads `gtk-4-1.dll` from `PATH`.
4. From the repository root:

   ```powershell
   dotnet run --project LueGtk\LueGtk.csproj
   ```

Optional TeX rendering uses the same `pdflatex` plus `pdftocairo` or ImageMagick tools on `PATH`.

## AppImage

On x86_64 Linux, with the .NET 8 SDK and the GTK 4 development files installed:

```bash
sudo apt install dotnet-sdk-8.0 libgtk-4-dev
LueGtk/packaging/build-appimage.sh
```

The script writes `LueGtk/dist/Lue-x86_64.AppImage` and copies it to `LueGtk/Lue-x86_64.AppImage`. That file bundles the editor, the .NET runtime, and GTK 4, so another machine does not need those installed. HarfBuzz, fontconfig, and FreeType are included beside GTK's GLib, so the GLib in the image is the one GObject calls. TeX lines still use `pdflatex` from the host when it is available. The bundled launcher uses the X11 backend.

Running an AppImage uses FUSE. On Debian and Ubuntu that package is `libfuse2`. When FUSE is not available, extract and run with `APPIMAGE_EXTRACT_AND_RUN=1`.

```bash
chmod +x LueGtk/dist/Lue-x86_64.AppImage
./LueGtk/dist/Lue-x86_64.AppImage
./LueGtk/dist/Lue-x86_64.AppImage notes.lue
```

## Tests

```bash
dotnet test LueGtk/LueGtk.sln
```

The tests cover theme loading, line classification, and the edit/insert/remove rules. They do not need a display.
