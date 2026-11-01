# the image library of Tesseract, StaxRip's OCR passes PNG files
version=1.87.0
sources="
https://github.com/DanBloomberg/leptonica/releases/download/1.87.0/leptonica-1.87.0.tar.gz c73363397f96eb1295602bf44d708a994ad42046c791bf03ea0505d829bdb6a7
"

build() {
    unpack leptonica-1.87.0.tar.gz
    cd leptonica-1.87.0
    cmake_build -DBUILD_SHARED_LIBS=OFF -DBUILD_PROG=OFF -DSW_BUILD=OFF -DENABLE_PNG=ON -DENABLE_ZLIB=ON \
        -DENABLE_GIF=OFF -DENABLE_JPEG=OFF -DENABLE_TIFF=OFF -DENABLE_WEBP=OFF -DENABLE_OPENJPEG=OFF
    stage_license leptonica-license.txt
}
