version=R1-89ca38a
packages="
RemoveGrain vs
"
depends="vapoursynth"
sources="
git https://github.com/vapoursynth/vs-removegrain.git 89ca38a6971e371bdce2778291393258daa5f03b
"

build() {
    unpack vs-removegrain-89ca38a6971e.tar
    cd vs-removegrain-89ca38a6971e
    meson_plugin
    plugin_install 'lib*.so*' Plugins/VS/RemoveGrainVS librgvs.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/RemoveGrainVS
}
