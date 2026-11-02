version=R4
packages="
DeScratch dual
"
depends="vapoursynth"
sources="
git https://github.com/vapoursynth/descratch.git 7522d184eead71d489910015ead2b61ff25f312b
"

build() {
    unpack descratch-7522d184eead.tar
    cd descratch-7522d184eead
    avs_export src/descratch.cpp
    meson_plugin
    plugin_install '*descratch*.so*' Plugins/Dual/DeScratch libDeScratch.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/Dual/DeScratch
}
