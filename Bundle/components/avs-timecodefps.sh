version=1.1.6
packages="
TimecodeFPS avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/TimecodeFPS.git f977b88c18f2629c4f2fe34aab16056d41ffb229
"

build() {
    unpack TimecodeFPS-f977b88c18f2.tar
    cd TimecodeFPS-f977b88c18f2
    cmake_plugin
    plugin_install 'libtimecodefps*.so*' Plugins/AVS/TimecodeFPS libTimecodeFPS.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/TimecodeFPS
}
