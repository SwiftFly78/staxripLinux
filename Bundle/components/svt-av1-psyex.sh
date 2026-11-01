# the PSYEX fork of SVT-AV1 by BlueSwordM
version=3.0.2-B
packages="SvtAv1EncApp (SVT-AV1-PSYEX)"
sources="
git https://github.com/BlueSwordM/svt-av1-psyex.git 3beb4aee471c686ecb05089b29b0e676026aea1b
"

build() {
    unpack svt-av1-psyex-3beb4aee471c.tar
    cd svt-av1-psyex-3beb4aee471c
    cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=OFF -DBUILD_DEC=OFF -DBUILD_TESTING=OFF
    cmake --build build
    install -D Bin/Release/SvtAv1EncApp $DESTDIR$PREFIX/svt-av1-psyex/SvtAv1EncApp
    stage_license LICENSE.md PATENTS.md
}

bundle() {
    apps_install Encoders/SvtAv1EncApp-Psyex svt-av1-psyex/SvtAv1EncApp
}
