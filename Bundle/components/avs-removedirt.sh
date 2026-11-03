version=1.1 # renovate: datasource=github-tags depName=pinterf/RemoveDirt extractVersion=^v(?<version>.+)$
packages="
RemoveDirt avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/RemoveDirt.git 91faf7aa3a432e7a3b58a703af228b6edd0e7d91
"

build() {
    unpack RemoveDirt-91faf7aa3a43.tar
    cd RemoveDirt-91faf7aa3a43
    cmake_plugin
    plugin_install 'libremovedirt*.so*' Plugins/AVS/RemoveDirt libRemoveDirt.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/RemoveDirt
}
