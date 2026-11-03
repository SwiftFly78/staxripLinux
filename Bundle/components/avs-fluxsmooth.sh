version=1.4-3287f81 # renovate: datasource=git-refs depName=https://github.com/pinterf/FluxSmooth.git branch=master digest=3287f81b7c6c0c2ba55fb373d3d7c28382043d95
packages="
FluxSmooth avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/FluxSmooth.git 3287f81b7c6c0c2ba55fb373d3d7c28382043d95
"

build() {
    unpack FluxSmooth-3287f81b7c6c.tar
    cd FluxSmooth-3287f81b7c6c
    cmake_plugin
    plugin_install 'libfluxsmooth*.so*' Plugins/AVS/FluxSmooth libFluxSmooth.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/FluxSmooth
}
