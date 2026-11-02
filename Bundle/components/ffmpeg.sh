version=9.0.2
packages="
ffmpeg
ffprobe
"
depends="x264 x265 svt-av1 aom xvid opus lame libvpx libtheora dav1d nv-codec-headers amf-headers libvpl avisynthplus"
sources="
https://ffmpeg.org/releases/ffmpeg-9.0.2.tar.xz 8c3850283eb25fa026482078a04051e0be17347b09ef81a0849bec15a96e002e
"

build() {
    unpack ffmpeg-9.0.2.tar.xz
    cd ffmpeg-9.0.2
    # no autodetection, so new packages in the build image don't change the build, the hardware encoders
    # load the libraries of the drivers
    configure_make --enable-gpl --enable-version3 --enable-shared --disable-static --disable-doc --disable-debug \
        --disable-autodetect --enable-bzlib --enable-iconv --enable-lzma --enable-zlib \
        --enable-libx264 --enable-libx265 --enable-libsvtav1 --enable-libaom --enable-libdav1d --enable-libvpx \
        --enable-libxvid --enable-libtheora --enable-libmp3lame --enable-libopus --enable-libvorbis \
        --enable-ffnvcodec --enable-nvenc --enable-nvdec --enable-cuvid --enable-amf --enable-libvpl --enable-vaapi \
        --enable-avisynth
    stage_license COPYING.GPLv3 LICENSE.md
}

bundle() {
    apps_install Encoders/ffmpeg bin/ffmpeg bin/ffprobe
}
