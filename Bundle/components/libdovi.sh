# the C library of dovi_tool, Cargo.lock pins the crates cargo downloads and verifies
version=3.4.0
depends="rust"
sources="
git https://github.com/quietvoid/dovi_tool.git d1abe0e27ff2c7ab3339614d06db9f8a058af6b2
"

build() {
    unpack dovi_tool-d1abe0e27ff2.tar
    cd dovi_tool-d1abe0e27ff2/dolby_vision
    cargo_c_install
    stage_license ../LICENSE
}
