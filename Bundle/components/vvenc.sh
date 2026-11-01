version=1.14.0
packages=vvencFFapp
sources="
git https://github.com/fraunhoferhhi/vvenc.git 9428ea8636ae7f443ecde89999d16b2dfc421524
"

build() {
    unpack vvenc-9428ea8636ae.tar
    cd vvenc-9428ea8636ae
    cmake_build -DBUILD_SHARED_LIBS=OFF -DVVENC_INSTALL_FULLFEATURE_APP=ON
    stage_license LICENSE.txt
}

bundle() {
    apps_install Encoders/vvencFFapp bin/vvencFFapp
}
