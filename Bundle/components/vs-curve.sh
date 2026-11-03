version=r3 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-Curve versioning=loose
packages="
Curve vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-Curve.git 5edf283f79a3df8eb6cef6156fa376747fb80d20
"

build() {
    unpack VapourSynth-Curve-5edf283f79a3.tar
    cd VapourSynth-Curve-5edf283f79a3
    meson_plugin
    plugin_install '*curve*.so*' Plugins/VS/Curve libCurve.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/Curve
}
