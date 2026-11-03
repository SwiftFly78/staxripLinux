version=11.0.1-56926fa # renovate: datasource=git-refs depName=https://github.com/Asd-g/vapoursynth-descale.git branch=master digest=56926fac62967bef15b52f56bfcafc136d61f903
packages="
Descale avs
Descale vs
"
depends="avisynthplus vapoursynth"
sources="
git https://github.com/Asd-g/vapoursynth-descale.git 56926fac62967bef15b52f56bfcafc136d61f903
"

build() {
    unpack vapoursynth-descale-56926fac6296.tar
    cd vapoursynth-descale-56926fac6296
    # one library for AviSynth+ and VapourSynth
    meson setup build --buildtype=release -Dlibtype=both
    ninja -C build
    plugin_install 'libdescale*.so*' Plugins/AVS/Descale libdescale.so
    plugin_install 'libdescale*.so*' Plugins/VS/Descale libdescale.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/Descale Plugins/VS/Descale
}
