# the SVT-AV1 encoder and its library for FFmpeg
version=4.2.0
packages="SvtAv1EncApp (SVT-AV1)"
sources="
git https://gitlab.com/AOMediaCodec/SVT-AV1.git 9292ec8e32bce26f781f277ec8739b53426c4300
"

build() {
    unpack SVT-AV1-9292ec8e32bc.tar
    cd SVT-AV1-9292ec8e32bc
    cmake_build -DBUILD_SHARED_LIBS=ON -DBUILD_DEC=OFF -DBUILD_TESTING=OFF
    stage_license LICENSE.md PATENTS.md
}

bundle() {
    apps_install Encoders/SvtAv1EncApp bin/SvtAv1EncApp
}
