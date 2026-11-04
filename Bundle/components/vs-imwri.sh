version=R2 # renovate: datasource=github-tags depName=vapoursynth/vs-imwri versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
libimwri vs
"
depends="vapoursynth imagemagick"
sources="
git https://github.com/vapoursynth/vs-imwri.git 3042a327739e44b929f5ab02ff1da4d8de5ee061
"

build() {
    unpack vs-imwri-3042a327739e.tar
    cd vs-imwri-3042a327739e
    # the static ImageMagick with the shared libraries of the image, the static ones Meson links aren't position
    # independent, and with the symbols of ImageMagick hidden
    g++ $CXXFLAGS -std=c++14 -msse2 -mfpmath=sse -fPIC -shared -fvisibility=hidden -I$PREFIX/include/vapoursynth \
        $(pkg-config --cflags Magick++) src/imwri.cpp -o libimwri.so $LDFLAGS -Wl,--exclude-libs,ALL \
        $(pkg-config --libs --static Magick++)
    plugin_install libimwri.so Plugins/VS/libimwri libimwri.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/libimwri
}
