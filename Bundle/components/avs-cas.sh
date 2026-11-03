# only Visual Studio projects upstream, built with port_plugin
version=1.0.1-a54f7b2 # renovate: datasource=git-refs depName=https://github.com/Asd-g/AviSynth-CAS.git branch=master digest=a54f7b29056629fdcbf9c50fbc7dc5dd33267a30
packages="
CAS avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-CAS.git a54f7b29056629fdcbf9c50fbc7dc5dd33267a30
"

build() {
    unpack AviSynth-CAS-a54f7b290566.tar
    cd AviSynth-CAS-a54f7b290566
    port_plugin libCAS.so
    plugin_install libCAS.so Plugins/AVS/CAS libCAS.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/CAS
}
