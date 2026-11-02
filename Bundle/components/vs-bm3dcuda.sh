version=2.17
packages="
BM3DCPU vs
"
depends="vapoursynth"
sources="
git https://github.com/WolframRhodium/VapourSynth-BM3DCUDA.git b28d362665a3eac755b84534650c85795414d50e
"

build() {
    unpack VapourSynth-BM3DCUDA-b28d362665a3.tar
    cd VapourSynth-BM3DCUDA-b28d362665a3
    cmake_plugin -DENABLE_CUDA=OFF -DENABLE_CPU=ON -DVAPOURSYNTH_INCLUDE_DIRECTORY=$PREFIX/include/vapoursynth
    plugin_install '*bm3dcpu*.so*' Plugins/VS/BM3DCUDA libbm3dcpu.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/BM3DCUDA
}
