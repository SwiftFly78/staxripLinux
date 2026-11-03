version=1.4 # renovate: datasource=github-tags depName=dwbuiten/d2vsource extractVersion=^v(?<version>.+)$
packages="
d2vsource vs
"
depends="vapoursynth ffmpeg"
sources="
git https://github.com/dwbuiten/d2vsource.git a14d66d5e107769bb79d8361e8c54102426270df
"

build() {
    unpack d2vsource-a14d66d5e107.tar
    cd d2vsource-a14d66d5e107
    meson_plugin
    plugin_install '*d2vsource*.so*' Plugins/VS/d2vsource libd2vsource.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/d2vsource
}
