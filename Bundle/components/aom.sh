# aomenc and the AV1 library of FFmpeg
version=3.15.3 # renovate: datasource=git-tags depName=https://aomedia.googlesource.com/aom extractVersion=^v(?<version>.+)$
packages=AOMEnc
sources="
https://storage.googleapis.com/aom-releases/libaom-3.15.3.tar.gz 12c01dc731ed7b5a999e28120b45bf58a002ba3435a4ba2cfbc1e084a27c9c04
"

build() {
    unpack libaom-3.15.3.tar.gz
    cd libaom-3.15.3
    cmake_build -DBUILD_SHARED_LIBS=ON -DENABLE_DOCS=OFF -DENABLE_TESTS=OFF -DENABLE_TOOLS=OFF
    stage_license LICENSE PATENTS
}

bundle() {
    apps_install Encoders/AOMEnc bin/aomenc
}
