version=4.0
packages="
Bifrost vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-bifrost.git 68cb17396a6cd40342f70fda3e7e0c4e9857be64
"

build() {
    unpack vapoursynth-bifrost-68cb17396a6c.tar
    cd vapoursynth-bifrost-68cb17396a6c
    meson_plugin
    plugin_install '*bifrost*.so*' Plugins/VS/Bifrost libbifrost.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/Bifrost
}
