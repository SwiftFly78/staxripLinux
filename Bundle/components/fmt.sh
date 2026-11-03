# the headers of {fmt} for std_format_shim, outside of the include folder of the prefix, builds that find them
# there would link the {fmt} library, which isn't built
version=12.1.0 # renovate: datasource=github-tags depName=fmtlib/fmt
sources="
git https://github.com/fmtlib/fmt.git 407c905e45ad75fc29bf0f9bb7c5c2fd3475976f
"

build() {
    unpack fmt-407c905e45ad.tar
    cd fmt-407c905e45ad
    mkdir -p $DESTDIR$PREFIX/share/fmt
    cp -r include $DESTDIR$PREFIX/share/fmt/
    stage_license LICENSE
}
