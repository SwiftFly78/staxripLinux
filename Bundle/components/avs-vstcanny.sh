version=1.1.8 # renovate: datasource=github-tags depName=Asd-g/AviSynth-vsTCanny
packages="
vsTCanny avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-vsTCanny.git d47eebe501d1bfb769f97e600b7fcfd5d2437a8a
"

build() {
    unpack AviSynth-vsTCanny-d47eebe501d1.tar
    cd AviSynth-vsTCanny-d47eebe501d1
    cmake_plugin
    plugin_install 'libvstcanny*.so*' Plugins/AVS/vsTCanny libvsTCanny.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/vsTCanny
}
