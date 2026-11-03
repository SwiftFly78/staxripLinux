version=0.95 # renovate: datasource=github-tags depName=pinterf/Average
packages="
Average avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/Average.git ec21ee1bdc4164cd3c98ba9b848179e77d083dd7
"

build() {
    unpack Average-ec21ee1bdc41.tar
    cd Average-ec21ee1bdc41
    cmake_plugin
    plugin_install 'libaverage*.so*' Plugins/AVS/Average libAverage.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/Average
}
