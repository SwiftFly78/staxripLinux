# fdkaac with a static build of the Fraunhofer FDK AAC library
version=1.0.9 # renovate: datasource=github-tags depName=nu774/fdkaac extractVersion=^v(?<version>.+)$ source=2
fdk_aac_version=3.0.0 # renovate: datasource=github-tags depName=mstorsjo/fdk-aac extractVersion=^v(?<version>.+)$ source=1
packages=fdkaac
sources="
git https://github.com/mstorsjo/fdk-aac.git a68a51aa45d86a43c7b228e3d45624af09dcdd8d
git https://github.com/nu774/fdkaac.git 1b311f7cda11ddabc11bb0e6230506796a6018c5
"

build() {
    local deps=$PWD/deps
    unpack fdk-aac-a68a51aa45d8.tar
    unpack fdkaac-1b311f7cda11.tar

    (cd fdk-aac-a68a51aa45d8 && cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=OFF \
        -DCMAKE_INSTALL_PREFIX=$deps -DCMAKE_INSTALL_LIBDIR=lib && cmake --build build && DESTDIR= cmake --install build)

    cd fdkaac-1b311f7cda11
    autoreconf -fi
    PKG_CONFIG_PATH=$deps/lib/pkgconfig:$PKG_CONFIG_PATH configure_make
    stage_license COPYING ../fdk-aac-a68a51aa45d8/NOTICE:fdk-aac-NOTICE
}

bundle() {
    apps_install Audio/fdkaac bin/fdkaac
}
