# the headers of {fmt} for std_format_shim, outside of the include folder of the prefix, builds that find them
# there would link the {fmt} library, which isn't built
version=12.2.1 # renovate: datasource=github-tags depName=fmtlib/fmt
sources="
git https://github.com/fmtlib/fmt.git 208219daff58f2a7438f6931cc2ddc358acf33ef
"

build() {
    unpack fmt-208219daff58.tar
    cd fmt-208219daff58
    mkdir -p $DESTDIR$PREFIX/share/fmt
    cp -r include $DESTDIR$PREFIX/share/fmt/
    stage_license LICENSE
}
