version=1.1-e45ab28
packages="
MedianBlur2 avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/MedianBlur2.git e45ab2874c9754f4bc519a1f1b0cb84d377eb431
"

build() {
    unpack MedianBlur2-e45ab2874c97.tar
    cd MedianBlur2-e45ab2874c97
    cmake_plugin
    plugin_install 'libmedianblur2*.so*' Plugins/AVS/MedianBlur2 libMedianBlur2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/MedianBlur2
}
