version=1.0.2-4f4b38b # renovate: datasource=git-refs depName=https://github.com/pinterf/GRunT.git branch=master digest=4f4b38b41ac71ed99ff9ba4103026beb1b1d9d27
packages="
GRunT avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/GRunT.git 4f4b38b41ac71ed99ff9ba4103026beb1b1d9d27
"

build() {
    unpack GRunT-4f4b38b41ac7.tar
    cd GRunT-4f4b38b41ac7/GRunT
    cmake_plugin
    plugin_install 'libgrunt*.so*' Plugins/AVS/GRunT libgrunt.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/GRunT
}
