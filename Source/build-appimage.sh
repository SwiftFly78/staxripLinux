#!/bin/sh
# Builds StaxRipLinux as AppImage that includes the .NET runtime, see "AppImage" in Docs/Introduction/Linux.md.
# usage: build-appimage.sh [<output folder>]
# Needs the .NET 10 SDK and appimagetool. APPIMAGETOOL overrides the appimagetool command, APPIMAGE_RUNTIME
# is the AppImage runtime file, without it appimagetool downloads the latest runtime.
set -eu

out=$(mkdir -p "${1:-.}" && cd "${1:-.}" && pwd) || exit 1
cd "$(dirname "$0")"

appimagetool=${APPIMAGETOOL:-appimagetool}

if ! command -v "$appimagetool" > /dev/null 2>&1; then
    echo "appimagetool is missing, it's available from https://github.com/AppImage/appimagetool/releases" >&2
    exit 1
fi

# the version the build shows, see build-info.sh
version=$(git describe --tags --match 'v[0-9]*' --dirty 2> /dev/null | tr -cd 'A-Za-z0-9.+-') || version=
version=${version#v}

tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
appdir=$tmp/StaxRipLinux.AppDir
lib=$appdir/usr/lib/staxrip

# own output folders, so the self-contained build doesn't mix with the builds in bin and obj
dotnet publish StaxRip.vbproj -c Release -r linux-x64 --self-contained -nologo \
    -p:OutputPath="$tmp/bin/" -p:BaseIntermediateOutputPath="$tmp/obj/" -o "$lib"

# the AppImage has its own desktop integration instead of the install script
mkdir -p "$appdir/usr/share/icons/hicolor/256x256/apps"
cp "$lib/Linux/staxrip.desktop" "$lib/Linux/staxrip.png" "$appdir/"
cp "$lib/Linux/staxrip.png" "$appdir/usr/share/icons/hicolor/256x256/apps/"
rm -r "$lib/Linux"

# exec keeps StaxRip as process name, job processing counts the running instances by name
cat > "$appdir/AppRun" << 'EOF'
#!/bin/sh
exec "$(dirname "$(readlink -f "$0")")/usr/lib/staxrip/StaxRip" "$@"
EOF
chmod +x "$appdir/AppRun"

file=$out/StaxRipLinux${version:+-$version}-x86_64.AppImage
ARCH=x86_64 VERSION=${version:-unknown} "$appimagetool" ${APPIMAGE_RUNTIME:+--runtime-file "$APPIMAGE_RUNTIME"} \
    "$appdir" "$file"
echo "$file"
