version=103.0 # renovate: datasource=gitea-tags depName=mbunkus/mkvtoolnix registryUrl=https://codeberg.org extractVersion=^release\-(?<version>.+)$
packages="
mkvmerge
mkvextract
mkvinfo
"
depends="qtbase"
sources="
https://mkvtoolnix.download/sources/mkvtoolnix-103.0.tar.xz d609bc395c409eade05d0a3a5aedd26e09739e232ed03b0f937cf8bd9448c590
"

build() {
    unpack mkvtoolnix-103.0.tar.xz
    cd mkvtoolnix-103.0
    # the man pages aren't built, configure requires DocBook anyway
    ./configure --prefix=$PREFIX --disable-gui --with-qmake6=$PREFIX/bin/qmake \
        --with-docbook-xsl-root=$PWD/no-docbook --with-xsltproc=/bin/true
    rake -j$JOBS apps:cli
    install -D -t $DESTDIR$PREFIX/bin src/mkvmerge src/mkvinfo src/mkvextract src/mkvpropedit
    stage_license COPYING
}

bundle() {
    apps_install Support/MKVToolNix bin/mkvmerge bin/mkvinfo bin/mkvextract bin/mkvpropedit
}
