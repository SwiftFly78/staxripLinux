# for libass
version=1.0.18 # renovate: datasource=github-releases depName=fribidi/fribidi extractVersion=^v(?<version>.+)$
sources="
https://github.com/fribidi/fribidi/releases/download/v1.0.18/fribidi-1.0.18.tar.xz 1de5e8073d035388b71d0b2d452b1da1f8caeea8a7702997b401fe1b6250727f
"

build() {
    unpack fribidi-1.0.18.tar.xz
    cd fribidi-1.0.18
    meson_build -Ddefault_library=static -Db_staticpic=true -Ddocs=false -Dbin=false -Dtests=false
    stage_license COPYING
}
