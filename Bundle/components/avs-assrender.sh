version=0.37 # renovate: datasource=github-tags depName=pinterf/assrender
packages="
AssRender avs
"
depends="avisynthplus libass"
sources="
git https://github.com/pinterf/assrender.git 5168977fd3296003f35b23cd04e45686b61ccfe3
"

build() {
    unpack assrender-5168977fd329.tar
    cd assrender-5168977fd329
    cmake_plugin
    plugin_install 'libassrender*.so*' Plugins/AVS/AssRender libassrender.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/AssRender
}
