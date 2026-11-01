# opusenc and opusdec with static builds of libopusenc and opusfile
version=0.2
packages="
OpusEnc
OpusDec
"
depends="opus"
sources="
git https://github.com/xiph/libopusenc.git 0dba1bea736ab8bb811409dce80c994a00a2ced9
https://downloads.xiph.org/releases/opus/opusfile-0.12.tar.gz 118d8601c12dd6a44f52423e68ca9083cc9f2bfe72da7a8c1acb22a80ae3550b
https://downloads.xiph.org/releases/opus/opus-tools-0.2.tar.gz b4e56cb00d3e509acfba9a9b627ffd8273b876b4e2408642259f6da28fa0ff86
"

build() {
    local deps=$PWD/deps
    unpack libopusenc-0dba1bea736a.tar
    unpack opusfile-0.12.tar.gz
    unpack opus-tools-0.2.tar.gz

    # the libraries are only linked into the tools, libopusenc 0.3 is the first one that builds with
    # opus 1.6, it has no release archive
    (cd libopusenc-0dba1bea736a && echo 'PACKAGE_VERSION="0.3"' > package_version && ./autogen.sh &&
        ./configure --prefix=$deps --disable-shared --with-pic --disable-doc --disable-examples &&
        make -j$JOBS && DESTDIR= make install)
    (cd opusfile-0.12 && ./configure --prefix=$deps --disable-shared --with-pic --disable-http --disable-doc \
        --disable-examples && make -j$JOBS && DESTDIR= make install)

    cd opus-tools-0.2
    PKG_CONFIG_PATH=$deps/lib/pkgconfig:$PKG_CONFIG_PATH configure_make
    stage_license COPYING ../libopusenc-0dba1bea736a/COPYING:libopusenc-COPYING ../opusfile-0.12/COPYING:opusfile-COPYING
}

bundle() {
    apps_install Audio/Opus bin/opusenc bin/opusdec
}
