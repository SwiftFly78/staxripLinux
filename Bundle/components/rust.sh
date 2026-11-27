# the Rust toolchain and cargo-c for the components written in Rust, only used to build them, the standard
# library of Rust is linked into their libraries
version=1.100.0 # renovate: datasource=github-tags depName=rust-lang/rust
cargo_c_version=0.10.26 # renovate: datasource=github-releases depName=lu-zero/cargo-c extractVersion=^v(?<version>.+)$ source=4
sources="
https://static.rust-lang.org/dist/rustc-1.100.0-x86_64-unknown-linux-gnu.tar.xz f1d0b1dec81dab6f523cd098507763487b0d4bd7fd2c87582f154128719be03b
https://static.rust-lang.org/dist/cargo-1.100.0-x86_64-unknown-linux-gnu.tar.xz 7e29ce07d7c6e92d1826870575ba27d57fff6cb3e754aa57d99099cb88fab35d
https://static.rust-lang.org/dist/rust-std-1.100.0-x86_64-unknown-linux-gnu.tar.xz 4591b6486594ded52ecd6135db95bf9355bbd3ca7a380888b141528bbcf9a5d4
https://github.com/lu-zero/cargo-c/releases/download/v0.10.26/cargo-c-x86_64-unknown-linux-musl.tar.gz 685fab5a2e1539c5075cea21c42469ee5a489d877dbdbf7adccf4bb631d77545
"
binary_sources="rustc-1.100.0-x86_64-unknown-linux-gnu.tar.xz cargo-1.100.0-x86_64-unknown-linux-gnu.tar.xz
rust-std-1.100.0-x86_64-unknown-linux-gnu.tar.xz cargo-c-x86_64-unknown-linux-musl.tar.gz"

build() {
    local c
    for c in rustc cargo rust-std; do
        unpack $c-1.100.0-x86_64-unknown-linux-gnu.tar.xz
        $c-1.100.0-x86_64-unknown-linux-gnu/install.sh --prefix=$PREFIX/rust --destdir=$DESTDIR --disable-ldconfig
    done

    unpack cargo-c-x86_64-unknown-linux-musl.tar.gz
    install -t $DESTDIR$PREFIX/rust/bin cargo-capi cargo-cbuild cargo-cinstall cargo-ctest
    stage_license rustc-1.100.0-x86_64-unknown-linux-gnu/LICENSE-APACHE rustc-1.100.0-x86_64-unknown-linux-gnu/LICENSE-MIT \
        rustc-1.100.0-x86_64-unknown-linux-gnu/COPYRIGHT
}
