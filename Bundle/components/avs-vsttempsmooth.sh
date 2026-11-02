version=1.4.0
packages="
vsTTempSmooth avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-vsTTempSmooth.git f393826b62521e8076dff5064b1bfcbcf8523220
"

build() {
    unpack AviSynth-vsTTempSmooth-f393826b6252.tar
    cd AviSynth-vsTTempSmooth-f393826b6252
    cmake_plugin
    plugin_install 'libvsttempsmooth*.so*' Plugins/AVS/vsTTempSmooth libvsTTempSmooth.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/vsTTempSmooth
}
