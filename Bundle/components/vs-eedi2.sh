version=r7.1 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-EEDI2 versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
EEDI2 vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-EEDI2.git 9d82bc56d6d4d48b7def216d9c8e52b237842242
"

build() {
    unpack VapourSynth-EEDI2-9d82bc56d6d4.tar
    cd VapourSynth-EEDI2-9d82bc56d6d4
    lock_thread_maps
    meson_plugin
    plugin_install '*eedi2*.so*' Plugins/VS/EEDI2 libEEDI2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/EEDI2
}
