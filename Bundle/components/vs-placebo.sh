version=3.3.1 # renovate: datasource=github-tags depName=sgt0/vs-placebo extractVersion=^v(?<version>.+)$
packages="
libvs_placebo vs
"
depends="vapoursynth libplacebo libdovi"
sources="
git https://github.com/sgt0/vs-placebo.git b6ca788bec16646456a88addd18b1f7c7fa07511
"

build() {
    unpack vs-placebo-b6ca788bec16.tar
    cd vs-placebo-b6ca788bec16
    meson_plugin
    plugin_install '*placebo*.so*' Plugins/VS/libvs_placebo libvs_placebo.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/libvs_placebo
}
