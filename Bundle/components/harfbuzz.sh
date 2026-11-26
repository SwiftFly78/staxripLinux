# for libass, without GLib
version=15.1.0 # renovate: datasource=github-releases depName=harfbuzz/harfbuzz
sources="
https://github.com/harfbuzz/harfbuzz/releases/download/15.1.0/harfbuzz-15.1.0.tar.xz 0fc7c565443a5f4be7da65ac5ee5bd737d9b158dc908708b7689a0a5796b0c8d
"

build() {
    unpack harfbuzz-15.1.0.tar.xz
    cd harfbuzz-15.1.0
    meson_build -Ddefault_library=static -Db_staticpic=true -Dfreetype=enabled -Dglib=disabled -Dgobject=disabled \
        -Dcairo=disabled -Dchafa=disabled -Dicu=disabled -Dgraphite2=disabled -Dtests=disabled -Ddocs=disabled \
        -Dutilities=disabled -Dintrospection=disabled
    stage_license COPYING
}
