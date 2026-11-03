# only Visual Studio projects upstream, built with port_plugin
version=0.2.2-a5070ad # renovate: datasource=git-refs depName=https://github.com/Asd-g/TEMmod.git branch=v8_interface digest=a5070ad45b3cb866f2b0df3ef910c54086a9ada5
packages="
TEMmod avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/TEMmod.git a5070ad45b3cb866f2b0df3ef910c54086a9ada5
"

build() {
    unpack TEMmod-a5070ad45b3c.tar
    cd TEMmod-a5070ad45b3c
    port_plugin libTEMmod.so
    plugin_install libTEMmod.so Plugins/AVS/TEMmod libTEMmod.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/TEMmod
}
