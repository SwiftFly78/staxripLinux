# the official build, a .NET app in a single file, Subtitle Edit 5 is under the MIT license
version=5.4.0 # renovate: datasource=github-releases depName=SubtitleEdit/subtitleedit extractVersion=^v(?<version>.+)$
packages="
Subtitle Edit
"
sources="
https://github.com/SubtitleEdit/subtitleedit/releases/download/v5.4.0/SubtitleEdit-Linux-x64.tar.gz e41ed6cc8b3d8b2ca69fdc6d573320900f0555a1ea5d2767d6ecc47353967548 SubtitleEdit-5.4.0-Linux-x64.tar.gz
https://github.com/SubtitleEdit/subtitleedit/archive/refs/tags/v5.4.0.tar.gz b4498121d002197a07173fd8a9039c3ada12f385075eb9bf554db280e8d83997 subtitleedit-5.4.0.tar.gz
"
binary_sources="SubtitleEdit-5.4.0-Linux-x64.tar.gz"

build() {
    mkdir se
    tar -C se -xf $C/downloads/SubtitleEdit-5.4.0-Linux-x64.tar.gz
    install -D -t $DESTDIR$PREFIX/subtitleedit se/SubtitleEdit se/libSkiaSharp.so se/libHarfBuzzSharp.so
    stage_license se/LICENSE
}

bundle() {
    local dir=$O/Apps/Support/SubtitleEdit
    mkdir -p $dir
    cp $PREFIX/subtitleedit/* $dir/
    # the command StaxRipLinux starts, Subtitle Edit runs ffmpeg and tesseract of the PATH
    cat > $dir/subtitleedit << 'EOF'
#!/bin/sh
dir=$(dirname "$(readlink -f "$0")")
export PATH="$dir/../../Encoders/ffmpeg:$dir/../../Subtitles/Tesseract:$PATH"
exec "$dir/SubtitleEdit" "$@"
EOF
    chmod +x $dir/subtitleedit
}
