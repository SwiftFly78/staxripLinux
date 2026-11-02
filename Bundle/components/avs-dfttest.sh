version=1.9.8
packages="
DFTTest avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/dfttest.git 465d5d184b1be47244b7f8b4f3da7a0d57aa4e29
"

build() {
    unpack dfttest-465d5d184b1b.tar
    cd dfttest-465d5d184b1b
    cmake_plugin
    plugin_install 'libdfttest*.so*' Plugins/AVS/DFTTest libdfttest.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/DFTTest
}
