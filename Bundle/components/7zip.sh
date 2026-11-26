# the static build of 7-Zip, named like the command StaxRip looks for
version=26.06 # renovate: datasource=github-releases depName=ip7z/7zip urlversion=nodots
packages=7zip
sources="
https://github.com/ip7z/7zip/releases/download/26.06/7z2606-linux-x64.tar.xz 2b539cf067b2ad48e1baa5093dbaa8818b73542d1222a0ffde9dc5f2922ea62e
https://github.com/ip7z/7zip/releases/download/26.06/7z2606-src.tar.xz a4e374c8f864f51df48c2cfd0e1413ec3b2ea030deae852d6a655402ebb61ac4
"
binary_sources="7z2606-linux-x64.tar.xz"

build() {
    mkdir 7z
    tar -C 7z -xf $C/downloads/7z2606-linux-x64.tar.xz
    install -D 7z/7zzs $DESTDIR$PREFIX/bin/7z
    stage_license 7z/License.txt
}

bundle() {
    apps_install Support/7zip bin/7z
}
