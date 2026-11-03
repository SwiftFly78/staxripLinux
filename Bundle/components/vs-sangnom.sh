version=45 # renovate: datasource=github-tags depName=dubhater/vapoursynth-sangnom extractVersion=^r(?<version>.+)$
packages="
Sangnom vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-sangnom.git 969c5135d6c62e377a25f3e8a9d0ee0f6f1ce1b4
"

build() {
    unpack vapoursynth-sangnom-969c5135d6c6.tar
    cd vapoursynth-sangnom-969c5135d6c6
    meson_plugin
    plugin_install '*sangnom*.so*' Plugins/VS/Sangnom libsangnom.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/Sangnom
}
