version=1 # renovate: datasource=github-tags depName=dubhater/vapoursynth-cnr2 extractVersion=^v(?<version>.+)$
packages="
CNR2 vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-cnr2.git efbc438526b49af4307f4530ba61426463462f6e
"

build() {
    unpack vapoursynth-cnr2-efbc438526b4.tar
    cd vapoursynth-cnr2-efbc438526b4
    # MAKEFLAGS of a parallel make end up in the compiler flags
    autoreconf -fi
    # cos of libm, which the VapourSynth process happens to load
    ./configure --prefix=$PREFIX LIBS=-lm
    make
    plugin_install '*cnr2*.so*' Plugins/VS/CNR2 libcnr2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/CNR2
}
