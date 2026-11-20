# the HDR fork of SVT-AV1 by juliobbv-p
version=2026-11-18-0ce7e13 # renovate: datasource=git-refs depName=https://github.com/juliobbv-p/svt-av1-hdr.git branch=main digest=0ce7e13af2396bf3bf06408a09f1138f82b01b28
packages="SvtAv1EncApp (SVT-AV1-HDR)"
depends="libdovi libhdr10plus"
sources="
git https://github.com/juliobbv-p/svt-av1-hdr.git 0ce7e13af2396bf3bf06408a09f1138f82b01b28
"

build() {
    unpack svt-av1-hdr-0ce7e13af239.tar
    cd svt-av1-hdr-0ce7e13af239
    # Dolby Vision and HDR10+ metadata with libdovi and libhdr10plus
    cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=OFF -DBUILD_DEC=OFF -DBUILD_TESTING=OFF \
        -DLIBDOVI_FOUND=ON -DLIBHDR10PLUS_RS_FOUND=ON
    cmake --build build
    install -D Bin/Release/SvtAv1EncApp $DESTDIR$PREFIX/svt-av1-hdr/SvtAv1EncApp
    stage_license LICENSE.md PATENTS.md
}

bundle() {
    apps_install Encoders/SvtAv1EncApp-Hdr svt-av1-hdr/SvtAv1EncApp
}
