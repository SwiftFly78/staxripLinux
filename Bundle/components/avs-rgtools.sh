version=1.3 # renovate: datasource=github-tags depName=pinterf/RgTools
packages="
RgTools avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/RgTools.git 207d6ad2aadebc25b9c6e0c76060df91e930a815
"

build() {
    unpack RgTools-207d6ad2aade.tar
    cd RgTools-207d6ad2aade
    cmake_plugin
    plugin_install 'librgtools*.so*' Plugins/AVS/RgTools libRgTools.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/RgTools
}
