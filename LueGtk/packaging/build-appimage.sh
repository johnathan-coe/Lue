#!/usr/bin/env bash
# Build a self-contained x86_64 AppImage of the Lue GTK editor.
# The image bundles the .NET runtime and GTK 4. TeX rendering still uses
# pdflatex from the host when a formula line is shown.
set -euo pipefail

ARCH="$(uname -m)"
if [[ "$ARCH" != "x86_64" ]]; then
    echo "This script builds an x86_64 AppImage. This machine is ${ARCH}." >&2
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
WORK="$REPO_ROOT/LueGtk/dist/appimage"
PUBLISH="$WORK/publish"
APPDIR="$WORK/AppDir"
CACHE="$WORK/cache"
LINUXDEPLOY="$CACHE/linuxdeploy-x86_64.AppImage"
GTK_PLUGIN="$CACHE/linuxdeploy-plugin-gtk.sh"

if [[ -x "$HOME/.dotnet/dotnet" ]]; then
    export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
    export PATH="$DOTNET_ROOT:$PATH"
fi
if ! command -v dotnet >/dev/null 2>&1; then
    echo "The .NET 8 SDK is required. Install it and re-run this script." >&2
    exit 1
fi
if ! pkg-config --exists gtk4; then
    echo "GTK 4 development files are required (pkg-config gtk4). On Debian/Ubuntu: sudo apt install libgtk-4-dev" >&2
    exit 1
fi
if ! command -v patchelf >/dev/null 2>&1 || ! command -v readelf >/dev/null 2>&1; then
    echo "patchelf and readelf are required. On Debian/Ubuntu: sudo apt install patchelf binutils" >&2
    exit 1
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export APPIMAGE_EXTRACT_AND_RUN=1
export DEPLOY_GTK_VERSION=4

mkdir -p "$CACHE" "$PUBLISH"
if [[ -d /usr/lib/x86_64-linux-gnu/gdk-pixbuf-2.0 ]]; then
    export PATH="/usr/lib/x86_64-linux-gnu/gdk-pixbuf-2.0:$PATH"
fi

if [[ ! -x "$LINUXDEPLOY" ]]; then
    curl -fL --retry 3 -o "$LINUXDEPLOY" \
        "https://github.com/linuxdeploy/linuxdeploy/releases/download/continuous/linuxdeploy-x86_64.AppImage"
    chmod +x "$LINUXDEPLOY"
fi
if [[ ! -x "$GTK_PLUGIN" ]]; then
    curl -fL --retry 3 -o "$GTK_PLUGIN" \
        "https://raw.githubusercontent.com/linuxdeploy/linuxdeploy-plugin-gtk/master/linuxdeploy-plugin-gtk.sh"
    chmod +x "$GTK_PLUGIN"
fi

echo "Publishing a self-contained linux-x64 build"
rm -rf "$PUBLISH"
dotnet publish "$REPO_ROOT/LueGtk/LueGtk.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o "$PUBLISH" \
    -p:DebugType=none \
    -p:DebugSymbols=false

# Optional diagnostics provider. It links LTTng, which the editor does not use.
rm -f "$PUBLISH/libcoreclrtraceptprovider.so"

rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/lib" "$APPDIR/apprun-hooks"
cp -a "$PUBLISH"/. "$APPDIR/usr/bin/"
# Keep documents, themes, fonts, and the logo out of usr/bin so the sidebar
# opens on "lue" instead of the runtime directory.
mkdir -p "$APPDIR/usr/share/lue"
for item in welcome.lue logo.png logo-icon.png themes Fonts; do
    if [[ -e "$APPDIR/usr/bin/$item" ]]; then
        mv "$APPDIR/usr/bin/$item" "$APPDIR/usr/share/lue/$item"
    fi
done
cp "$SCRIPT_DIR/00-lue-dark.sh" "$APPDIR/apprun-hooks/00-lue-dark.sh"
chmod +x "$APPDIR/apprun-hooks/00-lue-dark.sh"

# .NET loads ICU and OpenSSL with dlopen. Ship the sonames from this system
# so the image does not depend on the host's ICU or OpenSSL package.
copy_soname() {
    local name="$1"
    local path
    path="$(ldconfig -p | awk -v lib="$name" '$1 == lib { print $NF; exit }')"
    if [[ -z "$path" || ! -e "$path" ]]; then
        echo "Could not find $name on this system." >&2
        exit 1
    fi
    cp -L "$path" "$APPDIR/usr/lib/$name"
}
icu_uc="$(ldconfig -p | awk '$1 ~ /^libicuuc\.so\./ { print $1; exit }')"
icu_i18n="${icu_uc/libicuuc/libicui18n}"
icu_data="${icu_uc/libicuuc/libicudata}"
copy_soname "$icu_uc"
copy_soname "$icu_i18n"
copy_soname "$icu_data"
copy_soname libssl.so.3
copy_soname libcrypto.so.3

# The .NET host loads GTK and the runtime with dlopen, so linuxdeploy cannot
# see those libraries from the main executable alone.
library_args=()
while IFS= read -r -d '' shared; do
    library_args+=(--library "$shared")
done < <(find "$PUBLISH" -type f \( -name '*.so' -o -name '*.so.*' \) -print0)

echo "Bundling GTK 4 and the .NET runtime"
cd "$WORK"
rm -f "$WORK"/Lue*.AppImage
export PATH="$CACHE:$PATH"
"$LINUXDEPLOY" \
    --appdir "$APPDIR" \
    --executable "$APPDIR/usr/bin/LueGtk" \
    --desktop-file "$SCRIPT_DIR/org.lue.Editor.desktop" \
    --icon-file "$SCRIPT_DIR/lue.png" \
    "${library_args[@]}" \
    --plugin gtk \
    --output appimage

# linuxdeploy leaves "common" libraries on the host. GTK and Pango need
# HarfBuzz, and that host copy loads the host GLib. GObject 2.80 then calls
# a private helper at an offset that means something else on GLib 2.88.
# Copy every other non-GPU dependency next to GObject and point it at $ORIGIN
# so libglib-2.0.so.0 is the bundled 2.80 library.
bundle_private_libraries() {
    host_library() {
        case "$1" in
            libc.so.*|libm.so.*|libdl.so.*|libpthread.so.*|librt.so.*|libresolv.so.*|libutil.so.*|libgcc_s.so.*|libstdc++.so.*|ld-linux*.so.*|linux-vdso.so.*)
                return 0
                ;;
            libGL.so.*|libEGL.so.*|libGLdispatch.so.*|libGLX.so.*|libOpenGL.so.*|libvulkan.so.*|libdrm*.so.*|libgbm.so.*|libGLX_*.so.*|libEGL_*.so.*)
                return 0
                ;;
        esac
        return 1
    }

    needed_sonames() {
        readelf -d "$1" 2>/dev/null | awk '/\(NEEDED\)/ {
            line = $0
            sub(/.*\[/, "", line)
            sub(/\].*/, "", line)
            if (line != "") print line
        }'
    }

    source_for_soname() {
        local name="$1"
        local path found
        path="$(ldconfig -p | awk -v lib="$name" '$1 == lib { print $NF; exit }')"
        if [[ -n "$path" && -e "$path" ]]; then
            printf '%s\n' "$path"
            return 0
        fi
        found="$(find "$APPDIR" -name "$name" -print -quit)"
        if [[ -n "$found" ]]; then
            printf '%s\n' "$found"
            return 0
        fi
        return 1
    }

    local round=0 progress=1 elf needed src
    while [[ "$progress" -eq 1 ]]; do
        progress=0
        round=$((round + 1))
        if [[ "$round" -gt 15 ]]; then
            echo "Private library closure did not settle." >&2
            exit 1
        fi
        while IFS= read -r -d '' elf; do
            while IFS= read -r needed; do
                [[ -z "$needed" ]] && continue
                if host_library "$needed"; then
                    continue
                fi
                if [[ -e "$APPDIR/usr/lib/$needed" ]]; then
                    continue
                fi
                src="$(source_for_soname "$needed" || true)"
                if [[ -z "$src" ]]; then
                    echo "Could not find $needed, required by $elf" >&2
                    exit 1
                fi
                echo "Bundling $needed"
                cp -L "$src" "$APPDIR/usr/lib/$needed"
                patchelf --set-rpath '$ORIGIN' "$APPDIR/usr/lib/$needed"
                progress=1
            done < <(needed_sonames "$elf")
        done < <(find "$APPDIR" -type f -print0)
    done
}

assert_resolves_inside() {
    local elf="$1"
    local soname="$2"
    local resolved
    resolved="$(ldd "$elf" | awk -v name="$soname" '$1 == name { print $3; exit }')"
    case "$resolved" in
        "$APPDIR"/*) ;;
        *)
            echo "$elf resolved $soname to ${resolved:-nowhere}. It has to come from the AppDir." >&2
            exit 1
            ;;
    esac
}

bundle_private_libraries
assert_resolves_inside "$APPDIR/usr/lib/libgtk-4.so.1" libharfbuzz.so.0
assert_resolves_inside "$APPDIR/usr/lib/libgtk-4.so.1" libfontconfig.so.1
assert_resolves_inside "$APPDIR/usr/lib/libgtk-4.so.1" libfreetype.so.6
assert_resolves_inside "$APPDIR/usr/lib/libgtk-4.so.1" libglib-2.0.so.0
assert_resolves_inside "$APPDIR/usr/lib/libgobject-2.0.so.0" libglib-2.0.so.0
assert_resolves_inside "$APPDIR/usr/lib/libharfbuzz.so.0" libglib-2.0.so.0

toolroot="$CACHE/linuxdeploy-extracted"
appimagetool="$toolroot/plugins/linuxdeploy-plugin-appimage/appimagetool-prefix/usr/bin/appimagetool"
if [[ ! -x "$appimagetool" ]]; then
    rm -rf "$CACHE/squashfs-root" "$toolroot"
    ( cd "$CACHE" && "$LINUXDEPLOY" --appimage-extract )
    mv "$CACHE/squashfs-root" "$toolroot"
fi
rm -f "$WORK"/Lue*.AppImage
ARCH=x86_64 "$appimagetool" "$APPDIR" "$WORK/Lue-x86_64.AppImage"
chmod +x "$WORK/Lue-x86_64.AppImage"
cp -f "$WORK/Lue-x86_64.AppImage" "$REPO_ROOT/LueGtk/dist/Lue-x86_64.AppImage"
cp -f "$WORK/Lue-x86_64.AppImage" "$REPO_ROOT/LueGtk/Lue-x86_64.AppImage"
echo "Wrote $REPO_ROOT/LueGtk/Lue-x86_64.AppImage"
