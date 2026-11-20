# the SVT-AV1 encoder and its library for FFmpeg
version=4.3.0 # renovate: datasource=gitlab-tags depName=AOMediaCodec/SVT-AV1 extractVersion=^v(?<version>.+)$
packages="SvtAv1EncApp (SVT-AV1)"
sources="
git https://gitlab.com/AOMediaCodec/SVT-AV1.git e3d63405f9ba8219913a7276c2c47d10e5d022fe
"

build() {
    unpack SVT-AV1-e3d63405f9ba.tar
    cd SVT-AV1-e3d63405f9ba
    cmake_build -DBUILD_SHARED_LIBS=ON -DBUILD_DEC=OFF -DBUILD_TESTING=OFF
    stage_license LICENSE.md PATENTS.md
}

bundle() {
    apps_install Encoders/SvtAv1EncApp bin/SvtAv1EncApp
}
