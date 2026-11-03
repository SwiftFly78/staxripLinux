# only Visual Studio projects upstream, built with port_plugin
version=0.1.1-d5e0cdd # renovate: datasource=git-refs depName=https://github.com/Asd-g/ReduceFlicker.git branch=v8_interface digest=d5e0cdd397dcca9356683d7e40a3e595d78e35c8
packages="
ReduceFlicker avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/ReduceFlicker.git d5e0cdd397dcca9356683d7e40a3e595d78e35c8
"

build() {
    unpack ReduceFlicker-d5e0cdd397dc.tar
    cd ReduceFlicker-d5e0cdd397dc
    port_plugin libReduceFlicker.so
    plugin_install libReduceFlicker.so Plugins/AVS/ReduceFlicker libReduceFlicker.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/ReduceFlicker
}
