# the static build of the release, the sources contain the source code of the same release
version=2.3.4
packages="DoVi_Tool"
sources="
https://github.com/quietvoid/dovi_tool/releases/download/2.3.4/dovi_tool-2.3.4-x86_64-unknown-linux-musl.tar.gz 1844258e13c26607b32224bf1fa82b595d3b35949f5467405fda560daad32b3f
https://github.com/quietvoid/dovi_tool/archive/refs/tags/2.3.4.tar.gz 15b5cb68b3598e51ca968316443c9fb9597b6230e9d692cb4e641d54505a97ec dovi_tool-2.3.4.tar.gz
"
binary_sources="dovi_tool-2.3.4-x86_64-unknown-linux-musl.tar.gz"

build() {
    unpack dovi_tool-2.3.4-x86_64-unknown-linux-musl.tar.gz
    install -D dovi_tool $DESTDIR$PREFIX/bin/dovi_tool
    unpack dovi_tool-2.3.4.tar.gz
    stage_license dovi_tool-2.3.4/LICENSE
}

bundle() {
    apps_install Support/DoVi_Tool bin/dovi_tool
}
