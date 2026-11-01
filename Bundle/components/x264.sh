version=stable-b35605a
packages=x264
sources="
git https://code.videolan.org/videolan/x264.git b35605ace3ddf7c1a5d67a2eb553f034aef41d55
"

build() {
    unpack x264-b35605ace3dd.tar
    cd x264-b35605ace3dd
    configure_make --enable-shared --enable-pic --bit-depth=all --disable-lavf --disable-ffms --disable-gpac \
        --disable-lsmash
    stage_license COPYING
}

bundle() {
    apps_install Encoders/x264 bin/x264
}
