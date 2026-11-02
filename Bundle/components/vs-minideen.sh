version=r11
packages="
MiniDeen dual
"
depends="avisynthplus vapoursynth"
sources="
git https://github.com/HomeOfAviSynthPlusEvolution/MiniDeen.git 0342ed55d8cf2b5cb4077ff206d4e5bbaac734fa
"

build() {
    unpack MiniDeen-0342ed55d8cf.tar
    cd MiniDeen-0342ed55d8cf
    # the version comes from git describe, no TBB for the parallel algorithms
    git_version_tag r11
    cmake_plugin -DENABLE_PAR=OFF
    plugin_install '*neo-minideen*.so*' Plugins/Dual/MiniDeen libneo-minideen.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/Dual/MiniDeen
}
