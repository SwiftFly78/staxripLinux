version=0.11-86e048e
packages="
HQDN3D avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-hqdn3d.git 86e048e95be6ae946e34f226bc1b434911bc7fd1
"

build() {
    unpack AviSynth-hqdn3d-86e048e95be6.tar
    cd AviSynth-hqdn3d-86e048e95be6
    cmake_plugin
    plugin_install 'libhqdn3d*.so*' Plugins/AVS/HQDN3D libHqdn3d.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/HQDN3D
}
