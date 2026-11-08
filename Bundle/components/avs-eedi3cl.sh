version=1.1.2 # renovate: datasource=github-tags depName=Asd-g/AviSynthPlus-EEDI3CL
packages="
EEDI3CL avs
"
depends="avisynthplus fmt"
sources="
git https://github.com/Asd-g/AviSynthPlus-EEDI3CL.git b46bea5dbd164cee3c8b75f49739e2fe9b244691
"

build() {
    unpack AviSynthPlus-EEDI3CL-b46bea5dbd16.tar
    cd AviSynthPlus-EEDI3CL-b46bea5dbd16
    opencl_build_error src/EEDI3CL.cpp
    # the SIMD files use copyPad of EEDI3CL.cpp, where GCC inlines it without an instantiation the others could link
    cat >> src/EEDI3CL.cpp << 'EOF'

template void copyPad<uint8_t>(const AVS_VideoFrame*, AVS_VideoFrame*, const int, const int, const int, const bool,
    AVS_ScriptEnvironment* __restrict) noexcept;
template void copyPad<uint16_t>(const AVS_VideoFrame*, AVS_VideoFrame*, const int, const int, const int, const bool,
    AVS_ScriptEnvironment* __restrict) noexcept;
template void copyPad<float>(const AVS_VideoFrame*, AVS_VideoFrame*, const int, const int, const int, const bool,
    AVS_ScriptEnvironment* __restrict) noexcept;
EOF
    CXXFLAGS="$CXXFLAGS $(std_format_shim)" cmake_plugin
    plugin_install 'libeedi3cl*.so*' Plugins/AVS/EEDI3CL libeedi3cl.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/EEDI3CL
}
