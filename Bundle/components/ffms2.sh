# master, the last release 5.0 predates the API removals of FFmpeg 8 and 9
version=5.1.2-3af2ef2
packages="
ffms2 dual
ffmsindex
"
depends="ffmpeg"
sources="
git https://github.com/FFMS/ffms2.git 3af2ef2ae47bc30b64597c9e419e5b19c4bda7d8
"

build() {
    unpack ffms2-3af2ef2ae47b.tar
    cd ffms2-3af2ef2ae47b
    NOCONFIGURE=1 ./autogen.sh
    configure_make --enable-shared --disable-static
    stage_license COPYING
}

bundle() {
    apps_plugin Dual ffms2 lib/libffms2.so bin/ffmsindex
}
