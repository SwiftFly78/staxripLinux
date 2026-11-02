version=1.0.32
packages="
TIVTC avs
TDeint avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/TIVTC.git c383c9193cf19817422e2b28c537e8518cf100e3
"

build() {
    unpack TIVTC-c383c9193cf1.tar
    cd TIVTC-c383c9193cf1/src
    cmake_plugin
    plugin_install 'libtivtc*.so*' Plugins/AVS/TIVTC libTIVTC.so
    plugin_install 'libtdeint*.so*' Plugins/AVS/TDeint libTDeint.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/TIVTC Plugins/AVS/TDeint
}
