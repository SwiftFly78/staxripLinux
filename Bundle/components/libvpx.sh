# the VP8 and VP9 library of FFmpeg
version=1.17.0
sources="
git https://github.com/webmproject/libvpx.git 6df3ec34557879fff673706f4a1d9fbd0f3a6f0e
"

build() {
    unpack libvpx-6df3ec345578.tar
    cd libvpx-6df3ec345578
    ./configure --prefix=$PREFIX --enable-shared --disable-static --enable-pic --enable-vp9-highbitdepth \
        --disable-examples --disable-tools --disable-docs --disable-unit-tests --as=nasm
    make -j$JOBS
    make install
    stage_license LICENSE PATENTS
}
