version=r5 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-CTMF versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
CTMF vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-CTMF.git 073eccda90db402269e8796cc51fd29a3c7fcde5
"

build() {
    unpack VapourSynth-CTMF-073eccda90db.tar
    cd VapourSynth-CTMF-073eccda90db
    lock_thread_maps
    meson_plugin
    plugin_install '*ctmf*.so*' Plugins/VS/CTMF libCTMF.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/CTMF
}
