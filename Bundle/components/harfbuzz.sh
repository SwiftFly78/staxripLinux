# for libass, without GLib
version=15.0.0 # renovate: datasource=github-releases depName=harfbuzz/harfbuzz
sources="
https://github.com/harfbuzz/harfbuzz/releases/download/15.0.0/harfbuzz-15.0.0.tar.xz 968536e9abd782f07f7954044c0c4020199fcdd39b41d221ec42d2a3be4a44df
"

build() {
    unpack harfbuzz-15.0.0.tar.xz
    cd harfbuzz-15.0.0
    meson_build -Ddefault_library=static -Db_staticpic=true -Dfreetype=enabled -Dglib=disabled -Dgobject=disabled \
        -Dcairo=disabled -Dchafa=disabled -Dicu=disabled -Dgraphite2=disabled -Dtests=disabled -Ddocs=disabled \
        -Dutilities=disabled -Dintrospection=disabled
    stage_license COPYING
}
