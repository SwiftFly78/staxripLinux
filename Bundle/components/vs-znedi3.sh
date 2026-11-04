version=r3.3 # renovate: datasource=github-tags depName=sekrit-twc/znedi3 versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
znedi3 vs
"
depends="vapoursynth"
sources="
git https://github.com/sekrit-twc/znedi3.git fc540224e887dbce773c02c2eba22ca53252b614
"

build() {
    unpack znedi3-fc540224e887.tar
    cd znedi3-fc540224e887
    make -j$JOBS X86=1
    plugin_install 'vsznedi3*.so*' Plugins/VS/nnedi3 libvsznedi3.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/nnedi3
}
