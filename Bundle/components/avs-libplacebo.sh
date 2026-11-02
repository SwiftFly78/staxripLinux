version=1.6.0
packages="
AVS_LibPlacebo avs
"
depends="avisynthplus libplacebo libdovi"
sources="
git https://github.com/Asd-g/avslibplacebo.git d050318dacc487652a139b7b5fcbad5f8795ab6c
"

build() {
    unpack avslibplacebo-d050318dacc4.tar
    cd avslibplacebo-d050318dacc4
    # GCC 12 needs <cstring> for memcpy, which deband.cpp doesn't include
    CXXFLAGS="$CXXFLAGS -include cstring" cmake_plugin -DUSE_STATIC_LIBPLACEBO=OFF -DUSE_STATIC_DOVI=OFF -DUSE_STATIC_SHADERC=OFF
    plugin_install 'libavs_libplacebo*.so' Plugins/AVS/AVS_LibPlacebo libavs_libplacebo.so
    stage_license LICENSE
}

bundle() {
    plugin_bundle Plugins/AVS/AVS_LibPlacebo
}
