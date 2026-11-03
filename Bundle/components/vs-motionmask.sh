version=2 # renovate: datasource=github-tags depName=dubhater/vapoursynth-motionmask extractVersion=^v(?<version>.+)$
packages="
MotionMask vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-motionmask.git 5de6eb35dc80a2c47cedb5693ab0e6904ccdcdf6
"

build() {
    unpack vapoursynth-motionmask-5de6eb35dc80.tar
    cd vapoursynth-motionmask-5de6eb35dc80
    # GCC 12 needs <cstddef>, which the source doesn't include
    CXXFLAGS="$CXXFLAGS -include cstddef" meson_plugin
    plugin_install '*motionmask*.so*' Plugins/VS/MotionMask libmotionmask.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/MotionMask
}
