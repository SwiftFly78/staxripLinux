version=9.0.2
packages="
ffmpeg
ffprobe
"
depends="x264"
sources="
https://ffmpeg.org/releases/ffmpeg-9.0.2.tar.xz 8c3850283eb25fa026482078a04051e0be17347b09ef81a0849bec15a96e002e
"

build() {
    unpack ffmpeg-9.0.2.tar.xz
    cd ffmpeg-9.0.2
    # no autodetection, so new packages in the build image don't change the build
    configure_make --enable-gpl --enable-version3 --enable-shared --disable-static --disable-doc --disable-debug \
        --disable-autodetect --enable-bzlib --enable-iconv --enable-lzma --enable-zlib --enable-libx264
    stage_license COPYING.GPLv3 LICENSE.md
}

bundle() {
    apps_install Encoders/ffmpeg bin/ffmpeg bin/ffprobe
}
