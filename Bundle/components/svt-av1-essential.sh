# the Essential fork of SVT-AV1 by nekotrix
version=4.0.1-Essential # renovate: datasource=github-tags depName=nekotrix/SVT-AV1-Essential versioning=loose extractVersion=^v(?<version>.+)$
packages="SvtAv1EncApp (SVT-AV1-Essential)"
sources="
git https://github.com/nekotrix/SVT-AV1-Essential.git 7b064486c2f3996e9371593bb6c4ab6e1ba41b9f
"

build() {
    unpack SVT-AV1-Essential-7b064486c2f3.tar
    cd SVT-AV1-Essential-7b064486c2f3
    cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=OFF -DBUILD_DEC=OFF -DBUILD_TESTING=OFF
    cmake --build build
    install -D Bin/Release/SvtAv1EncApp $DESTDIR$PREFIX/svt-av1-essential/SvtAv1EncApp
    stage_license LICENSE.md PATENTS.md
}

bundle() {
    apps_install Encoders/SvtAv1EncApp-Essential svt-av1-essential/SvtAv1EncApp
}
