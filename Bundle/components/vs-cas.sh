version=r3 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-CAS versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
CAS vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-CAS.git 130108b30dd7ae1dfeebe9bb49c4683c639ef91d
"

build() {
    unpack VapourSynth-CAS-130108b30dd7.tar
    cd VapourSynth-CAS-130108b30dd7
    meson_plugin
    plugin_install 'cas.so' Plugins/VS/CAS libCAS.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/CAS
}
