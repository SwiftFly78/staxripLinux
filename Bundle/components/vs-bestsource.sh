# R22 and newer use functions of the VapourSynth API that R79 lacks
version=R21 # renovate: datasource=github-tags depName=vapoursynth/bestsource versioning=loose
packages="
BestSource dual
"
depends="vapoursynth ffmpeg"
sources="
git https://github.com/vapoursynth/bestsource.git 14c91f9fa74705facb251519096dc1b74a8632fe
"

build() {
    unpack bestsource-14c91f9fa747.tar
    cd bestsource-14c91f9fa747
    meson_plugin
    plugin_install '*bestsource*.so*' Plugins/Dual/BestSource libbestsource.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/Dual/BestSource
}
