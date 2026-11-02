# for libass
version=1.0.17
sources="
https://github.com/fribidi/fribidi/releases/download/v1.0.17/fribidi-1.0.17.tar.xz 6949dcde27d41cebad1fd741fcafc36d55a1020d2d872d4a6eb3914caabbada2
"

build() {
    unpack fribidi-1.0.17.tar.xz
    cd fribidi-1.0.17
    meson_build -Ddefault_library=static -Db_staticpic=true -Ddocs=false -Dbin=false -Dtests=false
    stage_license COPYING
}
