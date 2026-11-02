version=0.2.8
packages="
yadifmod2 avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/yadifmod2.git 9db5d2118dc2800701c5137afcfe45f3163211da
"

build() {
    unpack yadifmod2-9db5d2118dc2.tar
    cd yadifmod2-9db5d2118dc2
    cmake_plugin
    plugin_install 'libyadifmod2*.so*' Plugins/AVS/yadifmod2 libyadifmod2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/yadifmod2
}
