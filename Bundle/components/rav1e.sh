# the static build of the release, the sources contain the source code of the same release
version=0.8.1 # renovate: datasource=github-releases depName=xiph/rav1e extractVersion=^v(?<version>.+)$
packages="rav1e"
sources="
https://github.com/xiph/rav1e/releases/download/v0.8.1/rav1e-0.8.1-linux-generic.tar.gz 5e03b1c120e9cd113c790a08867f8ac944f612fab8a59c2e72f00e29a86c853d
https://github.com/xiph/rav1e/archive/refs/tags/v0.8.1.tar.gz 06d1523955fb6ed9cf9992eace772121067cca7e8926988a1ee16492febbe01e rav1e-0.8.1.tar.gz
"
binary_sources="rav1e-0.8.1-linux-generic.tar.gz"

build() {
    unpack rav1e-0.8.1-linux-generic.tar.gz
    install -D rav1e $DESTDIR$PREFIX/bin/rav1e
    unpack rav1e-0.8.1.tar.gz
    stage_license rav1e-0.8.1/LICENSE rav1e-0.8.1/PATENTS
}

bundle() {
    apps_install Encoders/Rav1e bin/rav1e
}
