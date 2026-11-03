# the static build of the release, the sources contain the source code of the same release
version=0.6.2 # renovate: datasource=github-releases depName=truehdd/truehdd
packages="Truehdd"
sources="
https://github.com/truehdd/truehdd/releases/download/0.6.2/truehdd-0.6.2-x86_64-unknown-linux-musl.tar.gz 1edef38a79107e4e70bfacfc9c758a77180145ec7fb3484c084efaac464d5806
https://github.com/truehdd/truehdd/archive/refs/tags/0.6.2.tar.gz d9e131bb7c312735a64f89a454dc935884a40ce74bfceb3082e6f28b2441a3b5 truehdd-0.6.2.tar.gz
"
binary_sources="truehdd-0.6.2-x86_64-unknown-linux-musl.tar.gz"

build() {
    unpack truehdd-0.6.2-x86_64-unknown-linux-musl.tar.gz
    install -D truehdd $DESTDIR$PREFIX/bin/truehdd
    unpack truehdd-0.6.2.tar.gz
    stage_license truehdd-0.6.2/LICENSE
}

bundle() {
    apps_install Audio/truehdd bin/truehdd
}
