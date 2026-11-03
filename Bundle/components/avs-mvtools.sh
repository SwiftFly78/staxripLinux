version=2.7.48 # renovate: datasource=github-tags depName=pinterf/mvtools
packages="
mvtools2 avs
DePan avs
DePanEstimate avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/mvtools.git a74dcb6418de723cc8f9eecb3c0c4f94032a925c
"

build() {
    unpack mvtools-a74dcb6418de.tar
    cd mvtools-a74dcb6418de
    cmake_plugin
    plugin_install 'libmvtools2*.so*' Plugins/AVS/mvtools2 libmvtools2.so
    plugin_install 'libdepan.so*' Plugins/AVS/MVTools2 libDePan.so
    plugin_install 'libdepanestimate*.so*' Plugins/AVS/MVTools2 libDePanEstimate.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/mvtools2 Plugins/AVS/MVTools2
}
