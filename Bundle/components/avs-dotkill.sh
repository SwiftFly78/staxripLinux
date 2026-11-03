version=1.0.0 # renovate: datasource=github-tags depName=Asd-g/AviSynth-DotKill
packages="
DotKill avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-DotKill.git 7113f582ecf7d3887ba0480f55e7bd839d9375ea
"

build() {
    unpack AviSynth-DotKill-7113f582ecf7.tar
    cd AviSynth-DotKill-7113f582ecf7
    cmake_plugin
    plugin_install 'libdotkill*.so*' Plugins/AVS/DotKill libDotKill.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/DotKill
}
