# the source of chikuzen's plugin from an archived collection of VapourSynth plugins
version=0.2.0 # renovate: none=the_plugin_has_no_repository_or_releases
packages="
scenechange vs
"
depends="vapoursynth"
sources="
https://raw.githubusercontent.com/darealshinji/vapoursynth-plugins/e5a0a74fc2651398508ef7fe44a07e1c58123bce/plugins/scenechange/src/scenechange.c bb9572f234f415a57fedac54a277f003ef4f91d23aba5f4454d813c29f977d17
"

build() {
    unpack scenechange.c
    gcc $CFLAGS -I$PREFIX/include/vapoursynth -fPIC -shared scenechange.c -o libscenechange.so $LDFLAGS
    plugin_install libscenechange.so Plugins/VS/scenechange libscenechange.so
    sed -n '1,/\*\//p' scenechange.c > LICENSE.txt
    stage_license LICENSE.txt
}

bundle() {
    plugin_bundle Plugins/VS/scenechange
}
