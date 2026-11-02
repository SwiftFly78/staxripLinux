version=4
packages="
LibDedot vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-dedot.git ce68b792f1b5487edd7a88462fcf6dd430ada2be
"

build() {
    unpack vapoursynth-dedot-ce68b792f1b5.tar
    cd vapoursynth-dedot-ce68b792f1b5
    meson_plugin
    plugin_install '*dedot*.so*' Plugins/VS/LibDedot libdedot.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/LibDedot
}
