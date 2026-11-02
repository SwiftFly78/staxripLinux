version=1.1.1e
packages="
KNLMeansCL dual
"
depends="avisynthplus vapoursynth"
sources="
git https://github.com/pinterf/KNLMeansCL.git c4af339b1d7b41268b3c5015627e68434bc139c7
"

build() {
    unpack KNLMeansCL-c4af339b1d7b.tar
    cd KNLMeansCL-c4af339b1d7b
    # the AviSynth+ interface is only compiled on Windows, and only with the header of an older AviSynth+
    sed -i -e '0,/^#ifdef _WIN32$/s//#if 1/' -e 's/^#ifdef __AVISYNTH_8_H__$/#if 1/' KNLMeansCL/NLMAvisynth.h
    sed -i 's/^#ifdef __AVISYNTH_8_H__$/#if 1/' KNLMeansCL/NLMAvisynth.cpp
    avs_export KNLMeansCL/NLMAvisynth.cpp
    CXXFLAGS="$CXXFLAGS -I$PREFIX/include/avisynth" meson_plugin
    plugin_install '*knlmeanscl*.so*' Plugins/Dual/KNLMeansCL libKNLMeansCL.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/Dual/KNLMeansCL
}
