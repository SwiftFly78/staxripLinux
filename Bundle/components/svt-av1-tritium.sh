# the Tritium fork of SVT-AV1 by Uranite
version=2026-11-19-af1bed1 # renovate: datasource=git-refs depName=https://github.com/Uranite/svt-av1-tritium.git branch=main digest=af1bed128da44e42f12fd77b7e4688c2072c744b
packages="SvtAv1EncApp (SVT-AV1-Tritium)"
depends="libdovi libhdr10plus"
sources="
git https://github.com/Uranite/svt-av1-tritium.git af1bed128da44e42f12fd77b7e4688c2072c744b
"

build() {
    unpack svt-av1-tritium-af1bed128da4.tar
    cd svt-av1-tritium-af1bed128da4
    # Dolby Vision and HDR10+ metadata with libdovi and libhdr10plus
    cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=OFF -DBUILD_DEC=OFF -DBUILD_TESTING=OFF \
        -DLIBDOVI_FOUND=ON -DLIBHDR10PLUS_RS_FOUND=ON
    cmake --build build
    install -D Bin/Release/SvtAv1EncApp $DESTDIR$PREFIX/svt-av1-tritium/SvtAv1EncApp
    stage_license LICENSE.md PATENTS.md
}

bundle() {
    apps_install Encoders/SvtAv1EncApp-Tritium svt-av1-tritium/SvtAv1EncApp
}
