# the AviSynth and VapourSynth scripts in Bundle/scripts, see the README there
version=2.52.6
sources=""

build() {
    :
}

bundle() {
    cp -r $B/scripts/Plugins $O/Apps/
}
