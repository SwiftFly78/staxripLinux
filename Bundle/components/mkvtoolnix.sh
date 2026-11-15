version=103.0 # renovate: datasource=gitea-tags depName=mbunkus/mkvtoolnix registryUrl=https://codeberg.org extractVersion=^release\-(?<version>.+)$
packages="
mkvmerge
mkvextract
mkvinfo
MKVToolnix GUI
"
depends="qtbase qtsvg qtmultimedia cmark"
sources="
https://mkvtoolnix.download/sources/mkvtoolnix-103.0.tar.xz d609bc395c409eade05d0a3a5aedd26e09739e232ed03b0f937cf8bd9448c590
"

build() {
    unpack mkvtoolnix-103.0.tar.xz
    cd mkvtoolnix-103.0
    # the resources of the GUI, like its icons, compressed with zlib, Qt Base is built without zstd
    sed -i "s/ compress-algo='zstd'//" src/mkvtoolnix-gui/qt_resources.qrc
    # the man pages aren't built, configure requires DocBook anyway, the GUI doesn't look for new versions online
    ./configure --prefix=$PREFIX --with-qmake6=$PREFIX/bin/qmake --disable-update-check \
        --with-docbook-xsl-root=$PWD/no-docbook --with-xsltproc=/bin/true
    rake -j$JOBS apps:cli apps:mkvtoolnix-gui src/mkvtoolnix-gui/qt_resources.rcc
    install -D -t $DESTDIR$PREFIX/bin src/mkvmerge src/mkvinfo src/mkvextract src/mkvpropedit \
        src/mkvtoolnix-gui/mkvtoolnix-gui
    install -D -t $DESTDIR$PREFIX/share/mkvtoolnix src/mkvtoolnix-gui/qt_resources.rcc
    stage_license COPYING
}

bundle() {
    apps_install Support/MKVToolNix bin/mkvmerge bin/mkvinfo bin/mkvextract bin/mkvpropedit
    qt_app_install Support/MKVToolNix bin/mkvtoolnix-gui
    # the GUI loads its resources from ../share/mkvtoolnix
    apps_install Support/share/mkvtoolnix share/mkvtoolnix/qt_resources.rcc
}
