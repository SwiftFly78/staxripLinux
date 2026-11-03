version=2.12 # renovate: datasource=github-tags depName=pinterf/fft3dfilter extractVersion=^v(?<version>.+)$
packages="
FFT3DFilter avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/fft3dfilter.git f5194457b30823b68e90b318be5710706e3bba77
"

build() {
    unpack fft3dfilter-f5194457b308.tar
    cd fft3dfilter-f5194457b308
    cmake_plugin
    plugin_install 'libfft3dfilter*.so*' Plugins/AVS/FFT3DFilter libfft3dfilter.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/FFT3DFilter
}
