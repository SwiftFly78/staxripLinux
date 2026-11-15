# FFmpeg 6.1 for D2V Witch, which uses API that FFmpeg 7 removed, only the parts it uses, static and in a prefix of
# its own, so the other components don't find it
version=6.1.6 # renovate: datasource=github-tags depName=FFmpeg/FFmpeg extractVersion=^n(?<version>6\.1\.\d+)$
sources="
https://ffmpeg.org/releases/ffmpeg-6.1.6.tar.xz d4fcb164028dd3beee5d92c0ac72e46aac6973c75ea12dc14de07bf8f407370a
"

build() {
    unpack ffmpeg-6.1.6.tar.xz
    cd ffmpeg-6.1.6
    ./configure --prefix=$PREFIX/ffmpeg6 --enable-static --disable-shared --enable-pic --disable-autodetect \
        --disable-programs --disable-doc --disable-everything --enable-protocol=file --enable-parsers \
        --enable-demuxer=mpegps,mpegts,mpegvideo,pva --enable-muxer=w64 \
        --enable-decoder=mpeg1video,mpeg2video,pcm_dvd,pcm_bluray,ac3,eac3,mp2,mp3,aac,dca,truehd,mlp
    make -j$JOBS
    make install
    stage_license COPYING.LGPLv2.1 LICENSE.md
}
