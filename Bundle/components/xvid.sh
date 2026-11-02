# xvid_encraw, the example encoder of xvidcore, and the library of FFmpeg
version=1.3.7
packages=xvid_encraw
sources="
https://downloads.xvid.com/downloads/xvidcore-1.3.7.tar.gz abbdcbd39555691dd1c9b4d08f0a031376a3b211652c0d8b3b8aa9be1303ce2d
"

build() {
    unpack xvidcore-1.3.7.tar.gz
    cd xvidcore/build/generic
    ./configure --prefix=$PREFIX
    make -j$JOBS
    # the makefile ignores DESTDIR and doesn't create the link libxvidcore.so, the static library could be
    # linked into FFmpeg instead of the shared one
    make install libdir=$DESTDIR$PREFIX/lib includedir=$DESTDIR$PREFIX/include
    rm $DESTDIR$PREFIX/lib/libxvidcore.a
    ln -sf libxvidcore.so.4 $DESTDIR$PREFIX/lib/libxvidcore.so
    cd ../../examples
    make xvid_encraw
    install -D xvid_encraw $DESTDIR$PREFIX/bin/xvid_encraw
    stage_license ../LICENSE
}

bundle() {
    apps_install Encoders/xvid_encraw bin/xvid_encraw
}
