#! /usr/bin/env bash
# The editor uses GTK's dark variant. Set the theme before the GTK AppImage
# hook chooses a light Adwaita theme on desktops with no color-scheme.
export APPIMAGE_GTK_THEME="${APPIMAGE_GTK_THEME:-Adwaita:dark}"
