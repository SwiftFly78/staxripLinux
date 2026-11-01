# the static build of the release, the sources contain the source code of the same release
version=1.7.2
packages="HDR10Plus_Tool"
sources="
https://github.com/quietvoid/hdr10plus_tool/releases/download/1.7.2/hdr10plus_tool-1.7.2-x86_64-unknown-linux-musl.tar.gz 06385f37a639d61ba21d4be3150c863846933bc3b58110e094d8fc8f1c2249f2
https://github.com/quietvoid/hdr10plus_tool/archive/refs/tags/1.7.2.tar.gz cc917e769bad85323c7f596179798cc96ac878a01ddfd53b210fecae4c891849 hdr10plus_tool-1.7.2.tar.gz
"
binary_sources="hdr10plus_tool-1.7.2-x86_64-unknown-linux-musl.tar.gz"

build() {
    unpack hdr10plus_tool-1.7.2-x86_64-unknown-linux-musl.tar.gz
    install -D hdr10plus_tool $DESTDIR$PREFIX/bin/hdr10plus_tool
    unpack hdr10plus_tool-1.7.2.tar.gz
    stage_license hdr10plus_tool-1.7.2/LICENSE
}

bundle() {
    apps_install Support/HDR10Plus_Tool bin/hdr10plus_tool
}
