# for the GUI of MKVToolNix, linked statically
version=0.31.2 # renovate: datasource=github-tags depName=commonmark/cmark
sources="
https://github.com/commonmark/cmark/archive/refs/tags/0.31.2.tar.gz f9bc5ca38bcb0b727f0056100fac4d743e768872e3bacec7746de28f5700d697 cmark-0.31.2.tar.gz
"

build() {
    unpack cmark-0.31.2.tar.gz
    cd cmark-0.31.2
    cmake_build -DBUILD_SHARED_LIBS=OFF -DBUILD_TESTING=OFF -DCMAKE_POSITION_INDEPENDENT_CODE=ON
    stage_license COPYING
}
