version=r30
packages="
fmtconv dual
"
depends="avisynthplus vapoursynth"
sources="
git https://github.com/EleonoreMizo/fmtconv.git e1b75cf7c62078ea981042c415a381e81a6bcca3
"

build() {
    unpack fmtconv-e1b75cf7c620.tar
    cd fmtconv-e1b75cf7c620/build/unix
    autotools_plugin
    plugin_install '*fmtconv*.so*' Plugins/Dual/fmtconv libfmtconv.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/Dual/fmtconv
}
