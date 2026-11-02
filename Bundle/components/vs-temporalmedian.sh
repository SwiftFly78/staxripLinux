version=1
packages="
TemporalMedian vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-temporalmedian.git 10e2be3112a47ec713c21fffd3d635efc7fac680
"

build() {
    unpack vapoursynth-temporalmedian-10e2be3112a4.tar
    cd vapoursynth-temporalmedian-10e2be3112a4
    meson_plugin
    plugin_install '*temporalmedian*.so*' Plugins/VS/TemporalMedian libtemporalmedian.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/TemporalMedian
}
