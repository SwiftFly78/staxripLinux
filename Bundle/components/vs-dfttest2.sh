version=11 # renovate: datasource=github-tags depName=AmusementClub/vs-dfttest2 extractVersion=^v(?<version>.+)$
packages="
dfttest2_CPU vs
dfttest2_GCC vs
"
depends="vapoursynth"
sources="
git https://github.com/AmusementClub/vs-dfttest2.git 00e384f22f241fde6d9b5b77995b046c73d88afd
"

build() {
    unpack vs-dfttest2-00e384f22f24.tar
    cd vs-dfttest2-00e384f22f24
    cmake_plugin -DENABLE_CUDA=OFF -DENABLE_CPU=ON -DENABLE_GCC=ON
    plugin_install '*dfttest2_cpu*.so*' Plugins/VS/DFTTest2 libdfttest2_cpu.so
    plugin_install '*dfttest2_gcc*.so*' Plugins/VS/DFTTest2 libdfttest2_gcc.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/DFTTest2
}
