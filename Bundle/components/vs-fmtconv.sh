# r31 is only on GitLab, the GitHub mirror stopped at r30
version=r31 # renovate: datasource=gitlab-tags depName=EleonoreMizo/fmtconv versioning=loose
packages="
fmtconv dual
"
depends="avisynthplus vapoursynth"
sources="
git https://gitlab.com/EleonoreMizo/fmtconv.git f841f5ff94fd701484041068bb7124f5f393f9e0
"

build() {
    unpack fmtconv-f841f5ff94fd.tar
    cd fmtconv-f841f5ff94fd/build/unix
    autotools_plugin
    plugin_install '*fmtconv*.so*' Plugins/Dual/fmtconv libfmtconv.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/Dual/fmtconv
}
