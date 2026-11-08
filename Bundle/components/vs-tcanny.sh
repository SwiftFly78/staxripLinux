version=r14 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-TCanny versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
TCanny vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-TCanny.git 14d7a15d9597fa33db3edd94238463a9ee97f1a7
"

build() {
    unpack VapourSynth-TCanny-14d7a15d9597.tar
    cd VapourSynth-TCanny-14d7a15d9597
    lock_thread_maps
    meson_plugin
    plugin_install '*tcanny*.so*' Plugins/VS/TCanny libTCanny.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/TCanny
}
