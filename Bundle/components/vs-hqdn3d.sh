version=r10-eb820cb # renovate: datasource=git-refs depName=https://github.com/Hinterwaeldlers/vapoursynth-hqdn3d.git branch=master digest=eb820cb23f7dc47eb67ea95def8a09ab69251d30
packages="
HQDN3D vs
"
depends="vapoursynth"
sources="
git https://github.com/Hinterwaeldlers/vapoursynth-hqdn3d.git eb820cb23f7dc47eb67ea95def8a09ab69251d30
"

build() {
    unpack vapoursynth-hqdn3d-eb820cb23f7d.tar
    cd vapoursynth-hqdn3d-eb820cb23f7d
    autotools_plugin
    plugin_install '*hqdn3d*.so*' Plugins/VS/HQDN3D libhqdn3d.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/HQDN3D
}
