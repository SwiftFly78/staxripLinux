# for vs-placebo, AVS_LibPlacebo, the encoders of rigaya and mpv, with the glslang of the build image, 7.360 because
# the encoders of rigaya rely on the structures of its API
version=7.360.1 # renovate: datasource=gitlab-tags depName=videolan/libplacebo registryUrl=https://code.videolan.org extractVersion=^v(?<version>.+)$
depends="libdovi"
sources="
git https://code.videolan.org/videolan/libplacebo.git cee9b076f2c63104ccfd497fa79c39a867293ec4
"

build() {
    unpack libplacebo-cee9b076f2c6.tar
    cd libplacebo-cee9b076f2c6
    meson_build -Dvulkan=enabled -Dglslang=enabled -Dshaderc=disabled -Dopengl=enabled -Dd3d11=disabled \
        -Dlcms=disabled -Ddovi=enabled -Dlibdovi=enabled -Ddemos=false -Dtests=false -Dunwind=disabled
    stage_license LICENSE
}
