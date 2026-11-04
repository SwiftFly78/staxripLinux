version=r10.1 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-Yadifmod versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
Yadifmod vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-Yadifmod.git 8aad01aeac6af00f78d5054c31ede75dffc52f03
"

build() {
    unpack VapourSynth-Yadifmod-8aad01aeac6a.tar
    cd VapourSynth-Yadifmod-8aad01aeac6a
    meson_plugin
    plugin_install '*yadifmod*.so*' Plugins/VS/Yadifmod libYadifmod.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/Yadifmod
}
