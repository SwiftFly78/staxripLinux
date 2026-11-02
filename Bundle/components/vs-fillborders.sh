version=4
packages="
FillBorders vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-fillborders.git 9cf9b51f2b99d883ebbb229018603f1bde63ec17
"

build() {
    unpack vapoursynth-fillborders-9cf9b51f2b99.tar
    cd vapoursynth-fillborders-9cf9b51f2b99
    meson_plugin
    plugin_install '*fillborders*.so*' Plugins/VS/FillBorders libfillborders.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/FillBorders
}
