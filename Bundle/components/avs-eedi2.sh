# only Visual Studio projects upstream, built with port_plugin
version=1.0.0-33e545f # renovate: datasource=git-refs depName=https://github.com/Asd-g/AviSynth-EEDI2.git branch=master digest=33e545f625e6aa1d327c5ddd611ed3c7670f49d1
packages="
EEDI2 avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-EEDI2.git 33e545f625e6aa1d327c5ddd611ed3c7670f49d1
"

build() {
    unpack AviSynth-EEDI2-33e545f625e6.tar
    cd AviSynth-EEDI2-33e545f625e6
    port_plugin libEEDI2.so
    plugin_install libEEDI2.so Plugins/AVS/EEDI2 libEEDI2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/EEDI2
}
