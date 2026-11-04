version=r10.1 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-TDeintMod versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
TDeintMod vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-TDeintMod.git 96418f06af4b02e781ff7a4a12c48a6dad474a06
"

build() {
    unpack VapourSynth-TDeintMod-96418f06af4b.tar
    cd VapourSynth-TDeintMod-96418f06af4b
    meson_plugin
    plugin_install '*tdeintmod*.so*' Plugins/VS/TDeintMod libTDeintMod.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/TDeintMod
}
