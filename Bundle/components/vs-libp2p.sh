version=r2
packages="
LibP2P vs
"
depends="vapoursynth"
sources="
git https://github.com/DJATOM/LibP2P-Vapoursynth.git 25dbd45683e955847a984e4c51017dce65c1773c
"

build() {
    unpack LibP2P-Vapoursynth-25dbd45683e9.tar
    cd LibP2P-Vapoursynth-25dbd45683e9
    # a static link of a shared module fails
    sed -i "/link_args: '-static'/d" meson.build
    meson_plugin
    plugin_install 'lib*.so*' Plugins/VS/LibP2P libLibP2P.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/LibP2P
}
