version=3
packages="
DeCross vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-decross.git 9b15aad15856b7b23bd8ddc2542fd156d09b891b
"

build() {
    unpack vapoursynth-decross-9b15aad15856.tar
    cd vapoursynth-decross-9b15aad15856
    meson_plugin
    plugin_install '*decross*.so*' Plugins/VS/DeCross libdecross.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/DeCross
}
