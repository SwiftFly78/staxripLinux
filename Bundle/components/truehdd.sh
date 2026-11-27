# the static build of the release, the sources contain the source code of the same release
version=0.6.3 # renovate: datasource=github-releases depName=truehdd/truehdd
packages="Truehdd"
sources="
https://github.com/truehdd/truehdd/releases/download/0.6.3/truehdd-0.6.3-x86_64-unknown-linux-musl.tar.gz 4286fe0581f4e489b9c7fc16e14eab3dc148d40115b01e5d79213eecbeac8026
https://github.com/truehdd/truehdd/archive/refs/tags/0.6.3.tar.gz 496b180c41b81e0e5cfd3152d5f6471ecbd749bef6113e462554e65dbe26c050 truehdd-0.6.3.tar.gz
"
binary_sources="truehdd-0.6.3-x86_64-unknown-linux-musl.tar.gz"

build() {
    unpack truehdd-0.6.3-x86_64-unknown-linux-musl.tar.gz
    install -D truehdd $DESTDIR$PREFIX/bin/truehdd
    unpack truehdd-0.6.3.tar.gz
    stage_license truehdd-0.6.3/LICENSE
}

bundle() {
    apps_install Audio/truehdd bin/truehdd
}
