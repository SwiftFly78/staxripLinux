# MP4Box of GPAC, only with the features for ISO media files
version=26.07.0 # renovate: datasource=github-tags depName=gpac/gpac extractVersion=^v(?<version>.+)$
packages=MP4Box
sources="
git https://github.com/gpac/gpac.git a07cbfff238a331233e11e916f9fb185d5da8604
"

build() {
    unpack gpac-a07cbfff238a.tar
    cd gpac-a07cbfff238a
    # --isomedia-only also disables the filters MP4Box imports and exports raw streams with
    ./configure --prefix=$PREFIX --static-bin --isomedia-only --enable-log --enable-threads --enable-ogg --enable-vobsub \
        $(for f in fin fout pin pout mp4dmx mp4mx reframer restamp unframer writegen inspect bsrw nhmlr nhmlw ttmlconv \
            rfac3 rfac4 rfadts rfav1 rfflac rflatm rfmhas rfmp3 rfmpgvid rfnalu rfpcm rfprores rftruehd \
            ufaac ufac4 ufm4v ufmhas ufnalu ufobu; do echo --enable-$f; done)
    make -j$JOBS
    install -D bin/gcc/MP4Box $DESTDIR$PREFIX/bin/MP4Box
    stage_license COPYING
}

bundle() {
    apps_install Support/MP4Box bin/MP4Box
}
