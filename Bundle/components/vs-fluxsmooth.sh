version=2
packages="
FluxSmooth vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-fluxsmooth.git a971d68874d1f40e3912598a929d6f2117473579
"

build() {
    unpack vapoursynth-fluxsmooth-a971d68874d1.tar
    cd vapoursynth-fluxsmooth-a971d68874d1
    autotools_plugin
    plugin_install '*fluxsmooth*.so*' Plugins/VS/FluxSmooth libfluxsmooth.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/FluxSmooth
}
