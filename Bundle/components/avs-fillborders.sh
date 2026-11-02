version=1.1.0-623891c
packages="
FillBorders avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-FillBorders.git 623891cc2bc7ee3844f00e677eea024c33aa32c9
"

build() {
    unpack AviSynth-FillBorders-623891cc2bc7.tar
    cd AviSynth-FillBorders-623891cc2bc7
    cmake_plugin
    plugin_install 'libfillborders*.so*' Plugins/AVS/FillBorders libFillBorders.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/FillBorders
}
