version=1
packages="
DegrainMedian vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-degrainmedian.git 99ddad85a2572a2ec03f859f9ed0e9d9627bfc11
"

build() {
    unpack vapoursynth-degrainmedian-99ddad85a257.tar
    cd vapoursynth-degrainmedian-99ddad85a257
    autotools_plugin
    plugin_install '*degrainmedian*.so*' Plugins/VS/DegrainMedian libdegrainmedian.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/DegrainMedian
}
