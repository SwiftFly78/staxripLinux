# the Tritium fork of SVT-AV1 by Uranite
version=2026-11-27-4931f28 # renovate: datasource=git-refs depName=https://github.com/Uranite/svt-av1-tritium.git branch=main digest=4931f28c5ba96c48652283badaa8cd97690a6555
packages="SvtAv1EncApp (SVT-AV1-Tritium)"
depends="libdovi libhdr10plus"
sources="
git https://github.com/Uranite/svt-av1-tritium.git 4931f28c5ba96c48652283badaa8cd97690a6555
"

build() {
    unpack svt-av1-tritium-4931f28c5ba9.tar
    cd svt-av1-tritium-4931f28c5ba9
    # the TF workaround of 2e3deaf uses TRUE, which only windows.h defines
    sed -i 's/|| TRUE)/|| true)/' Source/Lib/Codec/temporal_filtering.c
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
