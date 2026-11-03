#!/bin/bash
# Downloads the appimagetool and the AppImage runtime that build-appimage.sh takes with APPIMAGETOOL and
# APPIMAGE_RUNTIME, and verifies them: appimage-tools.sh <folder>
# Renovate updates the versions, Bundle/renovate.py post-upgrade the sources like the ones of the recipes.
set -euo pipefail

appimagetool_version=1.9.1 # renovate: datasource=github-releases depName=AppImage/appimagetool source=1
runtime_version=20251108 # renovate: datasource=github-releases depName=AppImage/type2-runtime source=2
sources="
https://github.com/AppImage/appimagetool/releases/download/1.9.1/appimagetool-x86_64.AppImage ed4ce84f0d9caff66f50bcca6ff6f35aae54ce8135408b3fa33abfc3cb384eb0 appimagetool
https://github.com/AppImage/type2-runtime/releases/download/20251108/runtime-x86_64 2fca8b443c92510f1483a883f60061ad09b46b978b2631c807cd873a47ec260d runtime-x86_64
"

while read -r url sum file; do
    [ -z "$url" ] && continue
    curl -fsSL --retry 5 --retry-all-errors -o "$1/$file" "$url"
    echo "$sum  $1/$file" | sha256sum -c --quiet
done <<< "$sources"
chmod +x "$1/appimagetool"
