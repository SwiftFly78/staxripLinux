version=1.3.0
packages="
D2VSource avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/MPEG2DecPlus.git 3bbe3269ca171bde0459d43da0e9753923a1f2d7
"

build() {
    unpack MPEG2DecPlus-3bbe3269ca17.tar
    cd MPEG2DecPlus-3bbe3269ca17
    cmake_plugin
    plugin_install 'libd2vsource*.so*' Plugins/AVS/D2VSource libD2VSource.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/D2VSource
}
