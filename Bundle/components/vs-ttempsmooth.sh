version=r4.1 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-TTempSmooth versioning=loose
packages="
TTempSmooth vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-TTempSmooth.git f26e5c8eafdf882fee20ef411b183baf4860b0d3
"

build() {
    unpack VapourSynth-TTempSmooth-f26e5c8eafdf.tar
    cd VapourSynth-TTempSmooth-f26e5c8eafdf
    meson_plugin
    plugin_install '*ttempsmooth*.so*' Plugins/VS/TTempSmooth libTTempSmooth.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/TTempSmooth
}
