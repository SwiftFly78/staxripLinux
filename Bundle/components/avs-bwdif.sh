version=1.3.0
packages="
Bwdif avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-BWDIF.git 16b286b61ed70fad58327c1c1fad866857fdab41
"

build() {
    unpack AviSynth-BWDIF-16b286b61ed7.tar
    cd AviSynth-BWDIF-16b286b61ed7
    cmake_plugin
    plugin_install 'libbwdif*.so*' Plugins/AVS/Bwdif libBWDIF.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/Bwdif
}
