version=2.2.30
packages="
masktools2 avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/masktools.git 44e178bd1ba25c8ced07bb9bda71bbdd46500746
"

build() {
    unpack masktools-44e178bd1ba2.tar
    cd masktools-44e178bd1ba2
    cmake_plugin
    plugin_install 'libmasktools2*.so*' Plugins/AVS/masktools2 libmasktools2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/masktools2
}
