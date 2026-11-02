# for vs-placebo, with the glslang of the build image, the Vulkan loader comes from the system
version=7.387.0
sources="
git https://code.videolan.org/videolan/libplacebo.git 275f7dea27924cdd34ab2e6c20e448b6bc4fab65
"

build() {
    unpack libplacebo-275f7dea2792.tar
    cd libplacebo-275f7dea2792
    meson_build -Dvulkan=enabled -Dglslang=enabled -Dshaderc=disabled -Dopengl=disabled -Dd3d11=disabled \
        -Dlcms=disabled -Ddovi=disabled -Dlibdovi=disabled -Ddemos=false -Dtests=false -Dunwind=disabled
    stage_license LICENSE
}
