version=0.9.0
packages="
neo-fft dual
"
depends="avisynthplus vapoursynth"
sources="
git https://github.com/HomeOfAviSynthPlusEvolution/neo-fft.git 1b627e473da58d55d73ef15f32cb6c15002757d9
"

build() {
    unpack neo-fft-1b627e473da5.tar
    cd neo-fft-1b627e473da5
    cmake_plugin
    plugin_install '*neo-fft*.so*' Plugins/Dual/neo-fft neo-fft.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/Dual/neo-fft
}
