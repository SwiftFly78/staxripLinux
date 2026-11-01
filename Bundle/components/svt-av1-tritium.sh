# the Tritium fork of SVT-AV1 by Uranite
version=2026-10-02-2f66694
packages="SvtAv1EncApp (SVT-AV1-Tritium)"
sources="
git https://github.com/Uranite/svt-av1-tritium.git 2f666940c0817145c1e995207fc69d2bb9847340
"

build() {
    unpack svt-av1-tritium-2f666940c081.tar
    cd svt-av1-tritium-2f666940c081
    cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=OFF -DBUILD_DEC=OFF -DBUILD_TESTING=OFF
    cmake --build build
    install -D Bin/Release/SvtAv1EncApp $DESTDIR$PREFIX/svt-av1-tritium/SvtAv1EncApp
    stage_license LICENSE.md PATENTS.md
}

bundle() {
    apps_install Encoders/SvtAv1EncApp-Tritium svt-av1-tritium/SvtAv1EncApp
}
