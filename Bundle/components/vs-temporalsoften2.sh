version=1
packages="
TemporalSoften2 vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-temporalsoften2.git 97e4817bcd8969bdbeb2da6ae4dec318e4bdbf7f
"

build() {
    unpack vapoursynth-temporalsoften2-97e4817bcd89.tar
    cd vapoursynth-temporalsoften2-97e4817bcd89
    meson_plugin
    plugin_install '*temporalsoften2*.so*' Plugins/VS/TemporalSoften2 libtemporalsoften2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/TemporalSoften2
}
