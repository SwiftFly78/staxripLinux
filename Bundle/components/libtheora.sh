# the Theora library of FFmpeg
version=1.2.0 # renovate: datasource=github-tags depName=xiph/theora extractVersion=^v(?<version>.+)$
sources="
https://downloads.xiph.org/releases/theora/libtheora-1.2.0.tar.xz ebdf77a8f5c0a8f7a9e42323844fa09502b34eb1d1fece7b5f54da41fe2122ec
"

build() {
    unpack libtheora-1.2.0.tar.xz
    cd libtheora-1.2.0
    configure_make --enable-shared --disable-static --disable-examples --disable-spec --disable-doc
    stage_license COPYING LICENSE
}
