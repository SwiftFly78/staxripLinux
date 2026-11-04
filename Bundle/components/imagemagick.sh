# ImageMagick 7 for libimwri: a static library with its configuration compiled in, which reads PNG, JPEG and TIFF
# with the libraries of the build image
version=7.1.2-33 # renovate: datasource=github-tags depName=ImageMagick/ImageMagick versioning=loose
sources="
git https://github.com/ImageMagick/ImageMagick.git 211e5b975a0019a88dcef5f84c1193f46738d815
"

build() {
    unpack ImageMagick-211e5b975a00.tar
    cd ImageMagick-211e5b975a00
    configure_make --disable-shared --enable-static --with-pic --enable-hdri --with-quantum-depth=16 \
        --enable-zero-configuration --without-modules --disable-openmp --disable-docs --without-utilities \
        --with-magick-plus-plus --without-perl --without-x --with-png --with-jpeg --with-tiff --with-lcms --with-zlib \
        --without-bzlib --without-lzma --without-zstd --without-zip --without-xml --without-fontconfig \
        --without-freetype --without-raqm --without-heic --without-jxl --without-openexr --without-openjp2 \
        --without-webp --without-raw --without-djvu --without-wmf --without-gvc --without-rsvg --without-pango \
        --without-fftw --without-fpx --without-jbig --without-lqr --without-gslib --without-dps --without-flif \
        --without-uhdr
    stage_license LICENSE NOTICE
}
