version=1.0.9 # renovate: datasource=github-tags depName=Asd-g/AviSynthPlus-NNEDI3CL
packages="
NNEDI3CL avs
"
depends="avisynthplus fmt"
sources="
git https://github.com/Asd-g/AviSynthPlus-NNEDI3CL.git bd454d1e9fea7c9f7e059f4e6abe17e67f0935d8
"

build() {
    unpack AviSynthPlus-NNEDI3CL-bd454d1e9fea.tar
    cd AviSynthPlus-NNEDI3CL-bd454d1e9fea
    opencl_build_error src/NNEDI3CL.cpp
    CXXFLAGS="$CXXFLAGS $(std_format_shim)" cmake_plugin
    plugin_install 'libnnedi3cl*.so*' Plugins/AVS/NNEDI3CL libnnedi3cl.so
    # it loads the weights next to it
    install -D -m 644 $(find . -name nnedi3_weights.bin | head -1) $DESTDIR$PREFIX/plugins/Plugins/AVS/NNEDI3CL/nnedi3_weights.bin
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/NNEDI3CL
}
