version=r8
packages="
nnedi3cl vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-NNEDI3CL.git eb2a810c0b7dfdd3ad908a1bdc07d6daab64eb57
"

build() {
    unpack VapourSynth-NNEDI3CL-eb2a810c0b7d.tar
    cd VapourSynth-NNEDI3CL-eb2a810c0b7d
    meson_plugin
    plugin_install '*nnedi3cl*.so*' Plugins/VS/nnedi3 libNNEDI3CL.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/nnedi3
}
