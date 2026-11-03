version=1.1 # renovate: datasource=github-tags depName=pinterf/TNLMeans extractVersion=^v(?<version>.+)$
packages="
TNLMeans avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/TNLMeans.git d2cb464b6fb852aacaca87b81140952e573a7a42
"

build() {
    unpack TNLMeans-d2cb464b6fb8.tar
    cd TNLMeans-d2cb464b6fb8
    cmake_plugin
    plugin_install 'libtnlmeans*.so*' Plugins/AVS/TNLMeans libTNLMeans.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/TNLMeans
}
