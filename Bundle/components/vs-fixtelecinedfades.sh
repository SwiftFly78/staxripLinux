version=r5-e70d7fb # renovate: datasource=git-refs depName=https://github.com/IFeelBloated/Fix-Telecined-Fades.git branch=master digest=e70d7fb759cf0109c8ed74a446c94124aa6b1201
packages="FixTelecinedFades vs"
depends="vapoursynth"
sources="
git https://github.com/IFeelBloated/Fix-Telecined-Fades.git e70d7fb759cf0109c8ed74a446c94124aa6b1201
"

build() {
    unpack Fix-Telecined-Fades-e70d7fb759cf.tar
    cd Fix-Telecined-Fades-e70d7fb759cf
    meson_plugin
    plugin_install libfixtelecinedfades.so Plugins/VS/FixTelecinedFades libftf_em64t_avx_fma.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/FixTelecinedFades
}
