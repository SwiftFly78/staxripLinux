# only Visual Studio projects upstream, built with port_plugin
version=0.5.1-6ee89c9
packages="
DCTFilter avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/DCTFilter.git 6ee89c9f92c981edbd1e67e4cd67866316671115
"

build() {
    unpack DCTFilter-6ee89c9f92c9.tar
    cd DCTFilter-6ee89c9f92c9
    port_plugin libDCTFilter.so
    plugin_install libDCTFilter.so Plugins/AVS/DCTFilter libDCTFilter.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/DCTFilter
}
