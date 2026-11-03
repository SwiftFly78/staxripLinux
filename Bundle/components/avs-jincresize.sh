version=2.1.4 # renovate: datasource=github-tags depName=Asd-g/AviSynth-JincResize
packages="
JincResize avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-JincResize.git 8b524b26401e5f8f3640fc13fe65672a1d47561a
"

build() {
    unpack AviSynth-JincResize-8b524b26401e.tar
    cd AviSynth-JincResize-8b524b26401e
    cmake_plugin
    plugin_install 'libjincresize*.so*' Plugins/AVS/JincResize libJincResize.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/JincResize
}
