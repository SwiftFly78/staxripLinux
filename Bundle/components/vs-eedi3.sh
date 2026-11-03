version=r10 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-EEDI3 versioning=loose
packages="
EEDI3m vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-EEDI3.git 11bb8f7eca7769ff3417c9d0be574f73e4b03426
"

build() {
    unpack VapourSynth-EEDI3-11bb8f7eca77.tar
    cd VapourSynth-EEDI3-11bb8f7eca77
    # GCC 12 needs <cstddef> for std::max_align_t, which the source doesn't include
    CXXFLAGS="$CXXFLAGS -include cstddef" meson_plugin
    plugin_install '*eedi3m*.so*' Plugins/VS/EEDI3m libEEDI3m.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/EEDI3m
}
