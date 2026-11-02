version=2022-02-10.AC3
packages="
vcmod vs
"
depends="vapoursynth"
sources="
git https://github.com/AmusementClub/vcm.git ff2d3cf04d048a975229c8aa0d73f82a73e5ed90
"

build() {
    unpack vcm-ff2d3cf04d04.tar
    cd vcm-ff2d3cf04d04
    meson_plugin
    plugin_install '*vcm*.so*' Plugins/VS/vcmod libvcmod.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/vcmod
}
