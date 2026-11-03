version=0.6.1 # renovate: datasource=github-tags depName=Asd-g/AviSynth-SangNom2
packages="
SangNom2 avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-SangNom2.git 3ad43bab1008f4fad3168bb0477008382cc2b627
"

build() {
    unpack AviSynth-SangNom2-3ad43bab1008.tar
    cd AviSynth-SangNom2-3ad43bab1008
    cmake_plugin
    plugin_install 'libsangnom2*.so*' Plugins/AVS/SangNom2 libSangNom2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/SangNom2
}
