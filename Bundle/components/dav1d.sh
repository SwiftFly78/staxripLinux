# the AV1 decoder of FFmpeg and ffms2
version=1.5.4
sources="
https://downloads.videolan.org/pub/videolan/dav1d/1.5.4/dav1d-1.5.4.tar.xz 686616b7c69eb88d44459391ab25cac13b6647a3b288835c5784e71c1514a5c5
"

build() {
    unpack dav1d-1.5.4.tar.xz
    cd dav1d-1.5.4
    meson_build -Denable_tools=false -Denable_tests=false
    stage_license COPYING
}
