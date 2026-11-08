version=r4.1 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-DeblockPP7 versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
DeblockPP7 vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-DeblockPP7.git 24bf2fc96a0a89a773538568884e07b18f9c4907
"

build() {
    unpack VapourSynth-DeblockPP7-24bf2fc96a0a.tar
    cd VapourSynth-DeblockPP7-24bf2fc96a0a
    lock_thread_maps
    meson_plugin
    plugin_install '*deblockpp7*.so*' Plugins/VS/DeblockPP7 libDeblockPP7.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/DeblockPP7
}
