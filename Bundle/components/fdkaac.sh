# fdkaac with a static build of the Fraunhofer FDK AAC library
version=1.0.9 # renovate: datasource=github-tags depName=nu774/fdkaac extractVersion=^v(?<version>.+)$ source=2
fdk_aac_version=2.0.3 # renovate: datasource=github-tags depName=mstorsjo/fdk-aac extractVersion=^v(?<version>.+)$ source=1
packages=fdkaac
sources="
git https://github.com/mstorsjo/fdk-aac.git 716f4394641d53f0d79c9ddac3fa93b03a49f278
git https://github.com/nu774/fdkaac.git 1b311f7cda11ddabc11bb0e6230506796a6018c5
"

build() {
    local deps=$PWD/deps
    unpack fdk-aac-716f4394641d.tar
    unpack fdkaac-1b311f7cda11.tar

    (cd fdk-aac-716f4394641d && cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=OFF \
        -DCMAKE_INSTALL_PREFIX=$deps -DCMAKE_INSTALL_LIBDIR=lib && cmake --build build && DESTDIR= cmake --install build)

    cd fdkaac-1b311f7cda11
    autoreconf -fi
    PKG_CONFIG_PATH=$deps/lib/pkgconfig:$PKG_CONFIG_PATH configure_make
    stage_license COPYING ../fdk-aac-716f4394641d/NOTICE:fdk-aac-NOTICE
}

bundle() {
    apps_install Audio/fdkaac bin/fdkaac
}
