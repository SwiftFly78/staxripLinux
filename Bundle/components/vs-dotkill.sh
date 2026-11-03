version=R4 # renovate: datasource=github-tags depName=myrsloik/DotKill versioning=loose
packages="
DotKill vs
"
depends="vapoursynth"
sources="
git https://github.com/myrsloik/DotKill.git 33f2915f28e38c4a68fc87af1b3d202f1d77db76
"

build() {
    unpack DotKill-33f2915f28e3.tar
    cd DotKill-33f2915f28e3
    meson_plugin
    plugin_install '*dotkill*.so*' Plugins/VS/DotKill libDotKill.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/DotKill
}
