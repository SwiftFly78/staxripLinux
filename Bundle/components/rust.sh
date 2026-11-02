# the Rust toolchain and cargo-c for the components written in Rust, only used to build them, the standard
# library of Rust is linked into their libraries
version=1.99.1
sources="
https://static.rust-lang.org/dist/rustc-1.99.1-x86_64-unknown-linux-gnu.tar.xz 2ff05ff9c08a90861e7f6e4065b40a8b87463b4576186fd613871c3e37f2091f
https://static.rust-lang.org/dist/cargo-1.99.1-x86_64-unknown-linux-gnu.tar.xz 53f3521949404d595efb06a55c12c5658d4ed1c75566f33d3d514eaebe70bd8a
https://static.rust-lang.org/dist/rust-std-1.99.1-x86_64-unknown-linux-gnu.tar.xz 109b074225bf5b9ea3820890876bfdf4ed161adbfaf94b2b4b5984c85ee5d958
https://github.com/lu-zero/cargo-c/releases/download/v0.10.26/cargo-c-x86_64-unknown-linux-musl.tar.gz 685fab5a2e1539c5075cea21c42469ee5a489d877dbdbf7adccf4bb631d77545
"
binary_sources="rustc-1.99.1-x86_64-unknown-linux-gnu.tar.xz cargo-1.99.1-x86_64-unknown-linux-gnu.tar.xz
rust-std-1.99.1-x86_64-unknown-linux-gnu.tar.xz cargo-c-x86_64-unknown-linux-musl.tar.gz"

build() {
    local c
    for c in rustc cargo rust-std; do
        unpack $c-1.99.1-x86_64-unknown-linux-gnu.tar.xz
        $c-1.99.1-x86_64-unknown-linux-gnu/install.sh --prefix=$PREFIX/rust --destdir=$DESTDIR --disable-ldconfig
    done

    unpack cargo-c-x86_64-unknown-linux-musl.tar.gz
    install -t $DESTDIR$PREFIX/rust/bin cargo-capi cargo-cbuild cargo-cinstall cargo-ctest
    stage_license rustc-1.99.1-x86_64-unknown-linux-gnu/LICENSE-APACHE rustc-1.99.1-x86_64-unknown-linux-gnu/LICENSE-MIT \
        rustc-1.99.1-x86_64-unknown-linux-gnu/COPYRIGHT
}
