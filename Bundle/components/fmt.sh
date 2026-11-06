# the headers of {fmt} for std_format_shim, outside of the include folder of the prefix, builds that find them
# there would link the {fmt} library, which isn't built
version=12.2.0 # renovate: datasource=github-tags depName=fmtlib/fmt
sources="
git https://github.com/fmtlib/fmt.git 1be298e1bd68957e4cd352e1f676f00e07dcfb57
"

build() {
    unpack fmt-1be298e1bd68.tar
    cd fmt-1be298e1bd68
    mkdir -p $DESTDIR$PREFIX/share/fmt
    cp -r include $DESTDIR$PREFIX/share/fmt/
    stage_license LICENSE
}
