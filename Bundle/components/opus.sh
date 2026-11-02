# libopus for opus-tools and FFmpeg
version=1.6.1
sources="
https://downloads.xiph.org/releases/opus/opus-1.6.1.tar.gz 6ffcb593207be92584df15b32466ed64bbec99109f007c82205f0194572411a1
"

build() {
    unpack opus-1.6.1.tar.gz
    cd opus-1.6.1
    configure_make --enable-shared --disable-static --disable-doc --disable-extra-programs
    stage_license COPYING
}
