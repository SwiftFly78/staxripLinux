# the subtitle renderer of the subtitle plugins
version=0.17.5
depends="fribidi harfbuzz"
sources="
https://github.com/libass/libass/releases/download/0.17.5/libass-0.17.5.tar.xz 2dca25c0e0c837ddf00b52011b3f82cac1e4ddd3ad018227806b0c2288864acc
"

build() {
    unpack libass-0.17.5.tar.xz
    cd libass-0.17.5
    # the version script of libass only exports its own functions, the static FriBidi and HarfBuzz stay private,
    # the plugins run in processes that loaded others already
    configure_make --enable-shared --disable-static
    stage_license COPYING
}
