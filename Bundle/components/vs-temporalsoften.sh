version=1.0 # renovate: datasource=github-tags depName=dubhater/vapoursynth-temporalsoften extractVersion=^v(?<version>.+)$
packages="
temporalsoften vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-temporalsoften.git 36a94c004d9e07db2f6a99130e6df9379930eb3c
"

build() {
    unpack vapoursynth-temporalsoften-36a94c004d9e.tar
    cd vapoursynth-temporalsoften-36a94c004d9e
    autotools_plugin
    plugin_install '*temporalsoften*.so*' Plugins/VS/temporalsoften libtemporalsoften.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/temporalsoften
}
