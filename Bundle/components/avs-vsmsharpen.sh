# only Visual Studio projects upstream, built with port_plugin
version=2.0.1-58036b3 # renovate: datasource=git-refs depName=https://github.com/Asd-g/AviSynth-vsMSharpen.git branch=master digest=58036b3b2a9e1b5fff7dad7b2295d7bde17c5929
packages="
vsMSharpen avs
"
depends="avisynthplus"
sources="
git https://github.com/Asd-g/AviSynth-vsMSharpen.git 58036b3b2a9e1b5fff7dad7b2295d7bde17c5929
"

build() {
    unpack AviSynth-vsMSharpen-58036b3b2a9e.tar
    cd AviSynth-vsMSharpen-58036b3b2a9e
    CXXFLAGS="$CXXFLAGS -DPORT_USING_STD_MINMAX" port_plugin libvsMSharpen.so
    plugin_install libvsMSharpen.so Plugins/AVS/vsMSharpen libvsMSharpen.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/vsMSharpen
}
