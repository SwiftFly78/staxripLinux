version=1.2 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-IT extractVersion=^v(?<version>.+)$
packages="
IT vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-IT.git e6a15aa67acb34609e1b072661beb6670b33a3b5
"

build() {
    unpack VapourSynth-IT-e6a15aa67acb.tar
    cd VapourSynth-IT-e6a15aa67acb
    # the makefile runs configure, which the archive has without the executable bit
    chmod +x configure
    make -j$JOBS
    plugin_install 'libit.so' Plugins/VS/IT libvs_it.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/IT
}
