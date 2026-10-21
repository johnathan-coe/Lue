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
mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/lib"
cp -a "$PUBLISH"/. "$APPDIR/usr/bin/"

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

produced="$(find "$WORK" -maxdepth 1 -name 'Lue*.AppImage' -printf '%f\n' | head -1)"
if [[ -z "$produced" ]]; then
    echo "linuxdeploy finished without writing an AppImage." >&2
    exit 1
fi
chmod +x "$WORK/$produced"
cp -f "$WORK/$produced" "$REPO_ROOT/LueGtk/dist/Lue-x86_64.AppImage"
echo "Wrote $REPO_ROOT/LueGtk/dist/Lue-x86_64.AppImage"
