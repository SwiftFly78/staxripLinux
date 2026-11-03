version=2 # renovate: datasource=github-tags depName=dubhater/vapoursynth-histogram extractVersion=^v(?<version>.+)$
packages="
libhistogram vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-histogram.git f67ec105c0e75812641b626046f2b671ab401b46
"

build() {
    unpack vapoursynth-histogram-f67ec105c0e7.tar
    cd vapoursynth-histogram-f67ec105c0e7
    autotools_plugin
    plugin_install '*histogram*.so*' Plugins/VS/libhistogram libhistogram.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/libhistogram
}
