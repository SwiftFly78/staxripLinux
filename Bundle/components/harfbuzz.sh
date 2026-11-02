# for libass, without GLib
version=14.6.0
sources="
https://github.com/harfbuzz/harfbuzz/releases/download/14.6.0/harfbuzz-14.6.0.tar.xz d07a007327277708a2a73ae437887cdbaf282937f6d03ca5467723e9099af586
"

build() {
    unpack harfbuzz-14.6.0.tar.xz
    cd harfbuzz-14.6.0
    meson_build -Ddefault_library=static -Db_staticpic=true -Dfreetype=enabled -Dglib=disabled -Dgobject=disabled \
        -Dcairo=disabled -Dchafa=disabled -Dicu=disabled -Dgraphite2=disabled -Dtests=disabled -Ddocs=disabled \
        -Dutilities=disabled -Dintrospection=disabled
    stage_license COPYING
}
