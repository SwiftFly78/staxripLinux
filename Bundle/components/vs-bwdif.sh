version=r5.1
packages="
Bwdif vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-Bwdif.git 8e12b5cbb18c64ee7638415a1452dc449986eab3
"

build() {
    unpack VapourSynth-Bwdif-8e12b5cbb18c.tar
    cd VapourSynth-Bwdif-8e12b5cbb18c
    meson_plugin
    plugin_install '*bwdif*.so*' Plugins/VS/Bwdif libBwdif.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/Bwdif
}
