version=r12
packages="
DFTTest Neo dual
"
depends="avisynthplus vapoursynth"
sources="
git https://github.com/HomeOfAviSynthPlusEvolution/neo_DFTTest.git 9f52824a901095a6e6394503e96dab40cfb579fb
"

build() {
    unpack neo_DFTTest-9f52824a9010.tar
    cd neo_DFTTest-9f52824a9010
    # the version comes from git describe, no TBB for the parallel algorithms
    git_version_tag r12
    cmake_plugin -DENABLE_PAR=OFF
    plugin_install '*neo-dfttest*.so*' 'Plugins/Dual/DFTTest Neo' libneo-dfttest.so
    stage_license_files
}

bundle() {
    plugin_bundle 'Plugins/Dual/DFTTest Neo'
}
