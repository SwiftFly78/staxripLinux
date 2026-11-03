version=1.1.2 # renovate: datasource=github-tags depName=Asd-g/libplacebo_Render
packages="
libplacebo_Render avs
"
depends="avisynthplus libplacebo libdovi fmt"
sources="
git https://github.com/Asd-g/libplacebo_Render.git 28db14efcfc87cbb30aaded4bb4402644fe79363
"
skip_submodules="dovi_tool libplacebo"

build() {
    unpack libplacebo_Render-28db14efcfc8.tar
    cd libplacebo_Render-28db14efcfc8
    CXXFLAGS="$CXXFLAGS $(std_format_shim)" cmake_plugin -DUSE_STATIC_LIBPLACEBO=OFF -DUSE_STATIC_DOVI=OFF
    plugin_install 'liblibplacebo_render*.so' Plugins/AVS/libplacebo_Render libplacebo_render.so
    # the help of the package, it documents the parameters
    install -m 644 README.md $DESTDIR$PREFIX/plugins/Plugins/AVS/libplacebo_Render/
    stage_license LICENSE
}

bundle() {
    plugin_bundle Plugins/AVS/libplacebo_Render
}
