#! /usr/bin/env bash
# The editor uses GTK's dark variant. Set the theme before the GTK AppImage
# hook chooses a light Adwaita theme on desktops with no color-scheme.
export APPIMAGE_GTK_THEME="${APPIMAGE_GTK_THEME:-Adwaita:dark}"

# The AppImage runtime sets APPDIR. An extracted tree sources this file from
# apprun-hooks/, one directory below the image root.
if [[ -z "${APPDIR:-}" ]]; then
    APPDIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
    export APPDIR
fi

# GObject in this image is 2.80 and calls into the GLib shipped beside it.
# Prefer that directory so a newer host GLib is not selected first.
case ":${LD_LIBRARY_PATH:-}:" in
    *":${APPDIR}/usr/lib:"*) ;;
    *) export LD_LIBRARY_PATH="${APPDIR}/usr/lib${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}" ;;
esac
