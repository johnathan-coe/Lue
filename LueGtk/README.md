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

The window uses the desktop GTK theme. There is no theme menu and no application stylesheet: headings use GTK’s `title-1`, `title-2`, and `title-3` styles, and the rest of the text uses the theme’s body style. File and Export behave like the Tkinter app. Export writes `rendered/index.html` and `rendered/index.css` beside the document, using the shared `themes/ms` stylesheet so the HTML matches the Python app’s default theme. Ctrl+S saves, Ctrl+N starts a new document, Ctrl+O opens, and Ctrl+Shift+S is Save As. Closing with unsaved edits asks whether to save.

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

## Tests

```bash
dotnet test LueGtk/LueGtk.sln
```

The tests cover theme loading for HTML export, line classification, the edit/insert/remove rules, and HTML export. They do not need a display.
