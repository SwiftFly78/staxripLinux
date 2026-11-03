# only a Visual Studio project upstream, built with port_plugin
version=0.9-31f0c5d # renovate: datasource=git-refs depName=https://github.com/tp7/checkmate.git branch=master digest=31f0c5d571c5d1ff682eba534a078f74cc017546
packages="
checkmate avs
"
depends="avisynthplus"
sources="
git https://github.com/tp7/checkmate.git 31f0c5d571c5d1ff682eba534a078f74cc017546
"

build() {
    unpack checkmate-31f0c5d571c5.tar
    cd checkmate-31f0c5d571c5
    # uses SSE4.1 without a check like the Windows build, but only includes the SSE2 header
    CXXFLAGS="$CXXFLAGS -msse4.1 -include smmintrin.h" port_plugin libcheckmate.so
    plugin_install libcheckmate.so Plugins/AVS/checkmate libcheckmate.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/checkmate
}
