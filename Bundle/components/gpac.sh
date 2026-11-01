# MP4Box of GPAC, only with the features for ISO media files
version=26.07.0
packages=MP4Box
sources="
git https://github.com/gpac/gpac.git a07cbfff238a331233e11e916f9fb185d5da8604
"

build() {
    unpack gpac-a07cbfff238a.tar
    cd gpac-a07cbfff238a
    ./configure --prefix=$PREFIX --static-bin --isomedia-only
    make -j$JOBS
    install -D bin/gcc/MP4Box $DESTDIR$PREFIX/bin/MP4Box
    stage_license COPYING
}

bundle() {
    apps_install Support/MP4Box bin/MP4Box
}
