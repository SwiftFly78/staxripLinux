version=r12 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-BM3D versioning=loose
packages="
BM3D vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-BM3D.git f85173a9db1716d1b1262cb48b25ae37281cbcec
"

build() {
    unpack VapourSynth-BM3D-f85173a9db17.tar
    cd VapourSynth-BM3D-f85173a9db17
    meson_plugin
    plugin_install '*bm3d*.so*' Plugins/VS/BM3D libBM3D.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/BM3D
}
