# the library StaxRip reads the media info with
version=26.10 # renovate: datasource=github-tags depName=MediaArea/MediaInfoLib extractVersion=^v(?<version>.+)$
packages=MediaInfo
depends="zenlib"
sources="
https://mediaarea.net/download/source/libmediainfo/26.10/libmediainfo_26.10.tar.xz a806fd36592549a03e810486f415a6858d4d8f14066bfbe97b32a447cd5eb590
"

build() {
    unpack libmediainfo_26.10.tar.xz
    cd MediaInfoLib/Project/CMake
    cmake_build -DBUILD_SHARED_LIBS=ON -DBUILD_ZLIB=OFF -DBUILD_ZENLIB=OFF
    stage_license ../../LICENSE
}

bundle() {
    apps_install Support/MediaInfo.NET lib/libmediainfo.so.0
}
