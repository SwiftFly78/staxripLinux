version=r3.2 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-DCTFilter versioning=loose
packages="
DCTFilter vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-DCTFilter.git 45bc72a5066ad88742ce3250754d3833d042dd5c
"

build() {
    unpack VapourSynth-DCTFilter-45bc72a5066a.tar
    cd VapourSynth-DCTFilter-45bc72a5066a
    meson_plugin
    plugin_install '*dctfilter*.so*' Plugins/VS/DCTFilter libDCTFilter.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/DCTFilter
}
