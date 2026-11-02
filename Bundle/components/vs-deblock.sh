version=r9
packages="
Deblock vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-Deblock.git 22296573c8a53631306591aabe4e6efd84c3a378
"

build() {
    unpack VapourSynth-Deblock-22296573c8a5.tar
    cd VapourSynth-Deblock-22296573c8a5
    meson_plugin
    plugin_install 'deblock.so' Plugins/VS/Deblock libDeblock.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/Deblock
}
