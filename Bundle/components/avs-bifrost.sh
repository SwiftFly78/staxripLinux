version=2.2-31cf836
packages="
Bifrost avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-bifrost.git 31cf83664f98406ed7bf12c0c63330d0fcaedb24
"

build() {
    unpack AviSynth-bifrost-31cf83664f98.tar
    cd AviSynth-bifrost-31cf83664f98
    cmake_plugin
    plugin_install 'libbifrost*.so*' Plugins/AVS/Bifrost libBifrost.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/Bifrost
}
