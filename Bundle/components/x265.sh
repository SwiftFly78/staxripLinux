version=4.2 # renovate: datasource=bitbucket-tags depName=multicoreware/x265_git
packages=x265
sources="
https://bitbucket.org/multicoreware/x265_git/downloads/x265_4.2.tar.gz 40b1ea0453e0309f0eba934e0ddf533f8f6295966679e8894e8f1c1c8d5e1210
"

build() {
    unpack x265_4.2.tar.gz
    cd x265_4.2/source

    # the executable and the library of FFmpeg support 8, 10 and 12 bit, like build/linux/multilib.sh
    local common="-G Ninja -DCMAKE_BUILD_TYPE=Release -DENABLE_HDR10_PLUS=ON -DENABLE_PIC=ON"
    cmake -S . -B ../b12 $common -DHIGH_BIT_DEPTH=ON -DMAIN12=ON -DEXPORT_C_API=OFF -DENABLE_SHARED=OFF \
        -DENABLE_CLI=OFF
    cmake --build ../b12
    cmake -S . -B ../b10 $common -DHIGH_BIT_DEPTH=ON -DEXPORT_C_API=OFF -DENABLE_SHARED=OFF -DENABLE_CLI=OFF
    cmake --build ../b10
    mkdir -p ../b8
    ln -s ../b10/libx265.a ../b8/libx265_main10.a
    ln -s ../b12/libx265.a ../b8/libx265_main12.a
    cmake -S . -B ../b8 $common -DCMAKE_INSTALL_PREFIX=$PREFIX -DENABLE_SHARED=ON \
        -DEXTRA_LIB="x265_main10.a;x265_main12.a" -DEXTRA_LINK_FLAGS=-L. -DLINKED_10BIT=ON -DLINKED_12BIT=ON
    cmake --build ../b8
    cmake --install ../b8
    stage_license ../COPYING
}

bundle() {
    apps_install Encoders/x265 bin/x265
}
