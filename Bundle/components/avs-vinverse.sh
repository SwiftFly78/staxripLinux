version=0.9.6 # renovate: datasource=github-tags depName=Asd-g/vinverse
packages="
vinverse avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/vinverse.git 8de84eea4c5618b1d7ad75742ba872e11f89c722
"

build() {
    unpack vinverse-8de84eea4c56.tar
    cd vinverse-8de84eea4c56
    # GCC 12 needs <limits>, which the source doesn't include
    CXXFLAGS="$CXXFLAGS -include limits" cmake_plugin
    plugin_install 'libvinverse*.so*' Plugins/AVS/vinverse libvinverse.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/vinverse
}
