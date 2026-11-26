version=2.3 # renovate: datasource=github-tags depName=pinterf/masktools
packages="
masktools2 avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/masktools.git 29a107486544d219bd2dba870a776d0e28f61660
"

build() {
    unpack masktools-29a107486544.tar
    cd masktools-29a107486544
    cmake_plugin
    plugin_install 'libmasktools2*.so*' Plugins/AVS/masktools2 libmasktools2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/masktools2
}
