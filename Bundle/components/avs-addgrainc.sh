version=1.8.4 # renovate: datasource=github-tags depName=pinterf/AddGrainC extractVersion=^v(?<version>.+)$
packages="
AddGrainC avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/AddGrainC.git 7e280a268ae5b7faaa03f7a3ce11ac626c86b35f
"

build() {
    unpack AddGrainC-7e280a268ae5.tar
    cd AddGrainC-7e280a268ae5
    cmake_plugin
    plugin_install 'libaddgrainc*.so*' Plugins/AVS/AddGrainC libAddGrainC.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/AddGrainC
}
