version=4
packages="
AWarpSharp2 vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-awarpsharp2.git 886d4b73ff1406e3be171bc141ff02c68addd774
"

build() {
    unpack vapoursynth-awarpsharp2-886d4b73ff14.tar
    cd vapoursynth-awarpsharp2-886d4b73ff14
    meson_plugin
    plugin_install '*awarpsharp2*.so*' Plugins/VS/AWarpSharp2 libawarpsharp2.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/AWarpSharp2
}
