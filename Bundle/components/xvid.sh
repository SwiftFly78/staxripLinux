# xvid_encraw is the example encoder of xvidcore
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
    cd ../../examples
    make xvid_encraw
    install -D xvid_encraw $DESTDIR$PREFIX/bin/xvid_encraw
    stage_license ../LICENSE
}

bundle() {
    apps_install Encoders/xvid_encraw bin/xvid_encraw
}
