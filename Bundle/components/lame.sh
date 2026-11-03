# the MP3 encoder library of FFmpeg
version=3.101 # renovate: datasource=repology depName=homebrew/lame
sources="
https://downloads.sourceforge.net/project/lame/lame/3.101/lame-3.101.tar.gz 7578af6eebd578b2bd64e468fac4ae1f03670a7e028166e67f855674b9b6aeac
"

build() {
    unpack lame-3.101.tar.gz
    cd lame-3.101
    configure_make --enable-shared --disable-static --disable-frontend --disable-decoder --disable-gtktest --enable-nasm
    stage_license COPYING
}
