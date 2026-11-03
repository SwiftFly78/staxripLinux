version=5.5.3 # renovate: datasource=github-tags depName=tesseract-ocr/tesseract
packages=Tesseract
depends="leptonica"
sources="
https://github.com/tesseract-ocr/tesseract/archive/refs/tags/5.5.3.tar.gz 9218e62793116d42a9f6d14cd9348518b27f382096eea3d0f2d1a24616bb5884 tesseract-5.5.3.tar.gz
"

build() {
    unpack tesseract-5.5.3.tar.gz
    cd tesseract-5.5.3
    cmake_build -DBUILD_SHARED_LIBS=OFF -DBUILD_TRAINING_TOOLS=OFF -DBUILD_TESTS=OFF -DDISABLE_CURL=ON \
        -DDISABLE_ARCHIVE=ON -DGRAPHICS_DISABLED=ON -DOPENMP_BUILD=OFF -DENABLE_NATIVE=OFF
    stage_license LICENSE
}

bundle() {
    apps_install Subtitles/Tesseract bin/tesseract
}
