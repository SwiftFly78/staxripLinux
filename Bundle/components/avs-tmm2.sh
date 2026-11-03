# only Visual Studio projects upstream, built with port_plugin
version=0.1.5 # renovate: datasource=github-tags depName=Asd-g/TMM2
packages="
TMM2 avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/TMM2.git 9c9100f378b9c2c3113b543a136b7892e93ca362
"

build() {
    unpack TMM2-9c9100f378b9.tar
    cd TMM2-9c9100f378b9
    port_plugin libTMM2.so
    plugin_install libTMM2.so Plugins/AVS/TMM2 libTMM2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/TMM2
}
