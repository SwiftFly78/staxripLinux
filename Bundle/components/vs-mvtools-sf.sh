# r9 like upstream StaxRip, later versions need vsFilterScript, whose repository is gone
version=r9 # renovate: none=later_versions_need_vsFilterScript_whose_repository_is_gone
packages="
mvtools-sf vs
"
depends="vapoursynth"
sources="
git https://github.com/IFeelBloated/vapoursynth-mvtools-sf.git 7d5ee292cdfcdbbc317cd7d8c1b026a7c7de27b6
"

build() {
    unpack vapoursynth-mvtools-sf-7d5ee292cdfc.tar
    cd vapoursynth-mvtools-sf-7d5ee292cdfc
    # its autotools files name sources that no longer exist
    g++ $CXXFLAGS -std=c++14 -msse2 -mfpmath=sse -fPIC -shared src/*.cpp -o libmvtools_sf_em64t.so $LDFLAGS -lfftw3
    plugin_install libmvtools_sf_em64t.so Plugins/VS/mvtools-sf libmvtools_sf_em64t.so
    printf '%s\n' "mvtools-sf is a single precision version of MVTools for VapourSynth. Its repository has no license" \
        "file, the GPL-2.0-or-later of MVTools, which it derives from, applies, see the license of vs-mvtools." \
        > LICENSE.txt
    stage_license LICENSE.txt
}

bundle() {
    plugin_bundle Plugins/VS/mvtools-sf
}
