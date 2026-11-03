# the C library of hdr10plus_tool, Cargo.lock pins the crates cargo downloads and verifies
version=2.1.5 # renovate: datasource=github-tags depName=quietvoid/hdr10plus_tool extractVersion=^libhdr10plus\-(?<version>.+)$
depends="rust"
sources="
git https://github.com/quietvoid/hdr10plus_tool.git ec9613eb257bd92130a446e2ce155685a243bd1d
"

build() {
    unpack hdr10plus_tool-ec9613eb257b.tar
    cd hdr10plus_tool-ec9613eb257b/hdr10plus
    cargo_c_install
    stage_license ../LICENSE
}
