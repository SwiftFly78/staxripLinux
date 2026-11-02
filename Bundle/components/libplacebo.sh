# for vs-placebo and AVS_LibPlacebo, with the glslang of the build image
version=7.387.0
depends="libdovi"
sources="
git https://code.videolan.org/videolan/libplacebo.git 275f7dea27924cdd34ab2e6c20e448b6bc4fab65
"

build() {
    unpack libplacebo-275f7dea2792.tar
    cd libplacebo-275f7dea2792
    meson_build -Dvulkan=enabled -Dglslang=enabled -Dshaderc=disabled -Dopengl=disabled -Dd3d11=disabled \
        -Dlcms=disabled -Ddovi=enabled -Dlibdovi=enabled -Ddemos=false -Dtests=false -Dunwind=disabled
    stage_license LICENSE
}
