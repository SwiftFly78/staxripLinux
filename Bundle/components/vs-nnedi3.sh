version=12
packages="
nnedi3 vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-nnedi3.git 8c35822ae358e46c99aae52e5a93010f1a5a6be6
"

build() {
    unpack vapoursynth-nnedi3-8c35822ae358.tar
    cd vapoursynth-nnedi3-8c35822ae358
    autotools_plugin
    plugin_install '*nnedi3*.so*' Plugins/VS/nnedi3 libnnedi3.so
    # the plugins of the folder load the weights next to them
    install -D -m 644 src/nnedi3_weights.bin $DESTDIR$PREFIX/plugins/Plugins/VS/nnedi3/nnedi3_weights.bin
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/nnedi3
}
