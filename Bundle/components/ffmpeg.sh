# 8.1, L-SMASH-Works uses AVCodecParser.split, which FFmpeg 9 removed
version=8.1.3
packages="
ffmpeg
ffprobe
"
depends="x264 x265 svt-av1 aom xvid opus lame libvpx libtheora dav1d nv-codec-headers amf-headers libvpl avisynthplus rubberband"
sources="
https://ffmpeg.org/releases/ffmpeg-8.1.3.tar.xz 7138d28c96d9d3e3af4ee3d8cad72741f8ffb40da90c1112235dea3ecd3178a3
"

build() {
    unpack ffmpeg-8.1.3.tar.xz
    cd ffmpeg-8.1.3
    # no autodetection, so new packages in the build image don't change the build, the hardware encoders
    # load the libraries of the drivers
    configure_make --enable-gpl --enable-version3 --enable-shared --disable-static --disable-doc --disable-debug \
        --disable-autodetect --enable-bzlib --enable-iconv --enable-lzma --enable-zlib \
        --enable-libx264 --enable-libx265 --enable-libsvtav1 --enable-libaom --enable-libdav1d --enable-libvpx \
        --enable-libxvid --enable-libtheora --enable-libmp3lame --enable-libopus --enable-libvorbis --enable-librubberband \
        --enable-ffnvcodec --enable-nvenc --enable-nvdec --enable-cuvid --enable-amf --enable-libvpl --enable-vaapi \
        --enable-avisynth
    stage_license COPYING.GPLv3 LICENSE.md
}

bundle() {
    apps_install Encoders/ffmpeg bin/ffmpeg bin/ffprobe
}
