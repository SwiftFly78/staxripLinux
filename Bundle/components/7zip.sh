# the static build of 7-Zip, named like the command StaxRip looks for
version=26.05
packages=7zip
sources="
https://github.com/ip7z/7zip/releases/download/26.05/7z2605-linux-x64.tar.xz 22cf82fcc6eb7474f1d5bb4f02c0f8ebd573a718089d6833dc30f8350a3b232e
https://github.com/ip7z/7zip/releases/download/26.05/7z2605-src.tar.xz 0b6080b8c3e7a579161d2d495224c0e51c467576cb4c7048b0ab485ef44cc22e
"
binary_sources="7z2605-linux-x64.tar.xz"

build() {
    mkdir 7z
    tar -C 7z -xf $C/downloads/7z2605-linux-x64.tar.xz
    install -D 7z/7zzs $DESTDIR$PREFIX/bin/7z
    stage_license 7z/License.txt
}

bundle() {
    apps_install Support/7zip bin/7z
}
