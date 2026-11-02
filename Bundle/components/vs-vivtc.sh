version=R2
packages="
VIVTC vs
"
depends="vapoursynth"
sources="
git https://github.com/vapoursynth/vivtc.git 4734c8b8526998b11bb2f313dfd0b4ecf760cfae
"

build() {
    unpack vivtc-4734c8b85269.tar
    cd vivtc-4734c8b85269
    meson_plugin
    plugin_install '*vivtc*.so*' Plugins/VS/VIVTC libVIVTC.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/VIVTC
}
