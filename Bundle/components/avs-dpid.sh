version=1.1.0 # renovate: datasource=github-tags depName=Asd-g/AviSynth-DPID
packages="
DPID avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-DPID.git 8f57f3c4e9007857220774a6a9f0f3c046efdeb1
"

build() {
    unpack AviSynth-DPID-8f57f3c4e900.tar
    cd AviSynth-DPID-8f57f3c4e900
    cmake_plugin
    plugin_install 'libdpid*.so*' Plugins/AVS/DPID libDPID.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/DPID
}
