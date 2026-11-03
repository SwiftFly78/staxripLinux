version=1.4 # renovate: none=APNG_Optimizer_has_no_releases_since_2016
packages=PNGopt
sources="
https://downloads.sourceforge.net/project/apng/APNG_Optimizer/1.4/apngopt-1.4-src.zip c96047a893645fbd4afa5631b5d1728db3e6874ba8d76637de34355294a38bcf
"

build() {
    mkdir apngopt
    cd apngopt
    unpack apngopt-1.4-src.zip
    make -j$JOBS
    install -D apngopt $DESTDIR$PREFIX/bin/apngopt
    stage_license readme.txt
}

bundle() {
    apps_install Thumbnails/PNGopt bin/apngopt
}
