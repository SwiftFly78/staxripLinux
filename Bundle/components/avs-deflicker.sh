# only Visual Studio projects upstream, built with port_plugin
version=0.6
packages="
DeFlicker avs
"
depends="avisynthplus"
sources="
git https://github.com/pinterf/Deflicker.git 7548bdd944910fef248e8d4d57e39907aa766f54
"

build() {
    unpack Deflicker-7548bdd94491.tar
    cd Deflicker-7548bdd94491
    CXXFLAGS="$CXXFLAGS -DPORT_MINMAX_MACROS" port_plugin libDeflicker.so
    plugin_install libDeflicker.so Plugins/AVS/DeFlicker libDeflicker.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/DeFlicker
}
