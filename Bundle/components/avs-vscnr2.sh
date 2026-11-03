version=1.0.2 # renovate: datasource=github-tags depName=Asd-g/AviSynth-vsCnr2
packages="
vsCnr2 avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-vsCnr2.git c6488ce9732cf1437db5f8e3b96b7ea8e4d7ff26
"

build() {
    unpack AviSynth-vsCnr2-c6488ce9732c.tar
    cd AviSynth-vsCnr2-c6488ce9732c
    cmake_plugin
    plugin_install 'libvscnr2*.so*' Plugins/AVS/vsCnr2 libvsCnr2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/vsCnr2
}
