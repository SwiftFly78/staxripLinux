version=1.3-21fb765
packages="
FrameRateConverter DLL avs
"
depends="avisynthplus"
sources="
git https://github.com/mysteryx93/FrameRateConverter.git 21fb7654e4c1b4899894692ab4ad24063643c496
"

build() {
    unpack FrameRateConverter-21fb7654e4c1.tar
    cd FrameRateConverter-21fb7654e4c1
    # the makefile adds -fPIC and the C++ standard only to the default flags
    make -j$JOBS avs CXXFLAGS="$CXXFLAGS -std=c++17 -fPIC"
    plugin_install 'libframerateconverter*.so*' Plugins/AVS/FrameRateConverter libFrameRateConverter-x64.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/FrameRateConverter
}
