version=5.5.4 # renovate: datasource=github-tags depName=tesseract-ocr/tesseract
packages=Tesseract
depends="leptonica"
sources="
https://github.com/tesseract-ocr/tesseract/archive/refs/tags/5.5.4.tar.gz a8dbf8b8c8ee427ff6ad1d99116502feaf59440af1adb893caf60a6576826cf4 tesseract-5.5.4.tar.gz
"

build() {
    unpack tesseract-5.5.4.tar.gz
    cd tesseract-5.5.4
    cmake_build -DBUILD_SHARED_LIBS=OFF -DBUILD_TRAINING_TOOLS=OFF -DBUILD_TESTS=OFF -DDISABLE_CURL=ON \
        -DDISABLE_ARCHIVE=ON -DGRAPHICS_DISABLED=ON -DOPENMP_BUILD=OFF -DENABLE_NATIVE=OFF
    stage_license LICENSE
}

bundle() {
    apps_install Subtitles/Tesseract bin/tesseract
}
