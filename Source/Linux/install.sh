#!/bin/sh
# Installs StaxRipLinux from the build output for the current user.
# PREFIX=/usr/local installs system wide, --uninstall removes the installation.
set -eu

app_dir=$(cd "$(dirname "$0")/.." && pwd)
prefix=${PREFIX:-$HOME/.local}
target=$prefix/lib/staxrip
icon_dir=$prefix/share/icons/hicolor/256x256/apps
desktop_dir=$prefix/share/applications

if [ "${1:-}" = "--uninstall" ]; then
    rm -rf "$target"
    rm -f "$prefix/bin/staxrip" "$desktop_dir/staxrip.desktop" "$icon_dir/staxrip.png"
    echo "StaxRipLinux was removed from $prefix"
    exit 0
fi

if [ ! -f "$app_dir/StaxRip.dll" ] || [ ! -x "$app_dir/StaxRip" ]; then
    echo "StaxRip.dll or StaxRip not found in $app_dir, run this script from the build output." >&2
    exit 1
fi

mkdir -p "$target" "$prefix/bin" "$icon_dir" "$desktop_dir"

# a Settings folder next to the executable enables portable mode, it's not installed
if [ "$app_dir" != "$target" ]; then
    (cd "$app_dir" && tar --exclude=./Settings -cf - .) | (cd "$target" && tar -xf -)
fi

ln -sf "$target/StaxRip" "$prefix/bin/staxrip"
cp "$app_dir/Linux/staxrip.png" "$icon_dir/staxrip.png"
sed "s|^Exec=.*|Exec=\"$prefix/bin/staxrip\" %F|" "$app_dir/Linux/staxrip.desktop" > "$desktop_dir/staxrip.desktop"

if command -v update-desktop-database > /dev/null 2>&1; then
    update-desktop-database "$desktop_dir" > /dev/null 2>&1 || true
fi

echo "StaxRipLinux was installed to $target"
