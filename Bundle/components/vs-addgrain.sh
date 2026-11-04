version=r10 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-AddGrain versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
AddGrain vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-AddGrain.git 01d8bf41a6330648a5a74c9501baf71667371c40
"

build() {
    unpack VapourSynth-AddGrain-01d8bf41a633.tar
    cd VapourSynth-AddGrain-01d8bf41a633
    meson_plugin
    plugin_install '*addgrain*.so*' Plugins/VS/AddGrain libAddGrain.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/AddGrain
}
