# L-SMASH-Works for AviSynth+ and VapourSynth in one library, with the FFmpeg of the bundle instead of the
# patched FFmpeg its builds use
version=1310-7e63c84
packages="
L-SMASH-Works dual
"
depends="avisynthplus vapoursynth ffmpeg dav1d libvpx"
sources="
git https://github.com/HomeOfAviSynthPlusEvolution/L-SMASH-Works.git 7e63c8400a252f4dbbeea80cc3bfec895abf12cf
"

build() {
    unpack L-SMASH-Works-7e63c8400a25.tar
    cd L-SMASH-Works-7e63c8400a25
    cmake_plugin -DBUILD_AVS_PLUGIN=ON -DBUILD_VS_PLUGIN=ON -Ddav1d_USE_STATIC_LIBS=OFF -DVPX_USE_STATIC_LIBS=OFF \
        -DZLIB_USE_STATIC_LIBS=OFF -DENABLE_MFX=OFF -DENABLE_XML2=OFF -DCMAKE_POSITION_INDEPENDENT_CODE=ON
    plugin_install libLSMASHSource.so Plugins/Dual/L-SMASH-Works libvslsmashsource.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/Dual/L-SMASH-Works
}
