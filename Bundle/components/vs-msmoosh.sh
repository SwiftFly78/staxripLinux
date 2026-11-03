version=1.1 # renovate: datasource=github-tags depName=dubhater/vapoursynth-msmoosh extractVersion=^v(?<version>.+)$
packages="
msmoosh vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-msmoosh.git e5a7221e0bcf99dcee026e5f2b8585c8daca5638
"

build() {
    unpack vapoursynth-msmoosh-e5a7221e0bcf.tar
    cd vapoursynth-msmoosh-e5a7221e0bcf
    autotools_plugin
    plugin_install '*msmoosh*.so*' Plugins/VS/msmoosh libmsmoosh.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/msmoosh
}
