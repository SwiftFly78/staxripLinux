version=29.2 # renovate: datasource=github-tags depName=dubhater/vapoursynth-mvtools versioning=regex:^v?(?<major>\d+)[._](?<minor>\d+)$ tag=v{version_}
packages="
mvtools vs
"
depends="vapoursynth"
sources="
git https://github.com/dubhater/vapoursynth-mvtools.git 17250aa979616ac48dfb0e18abfdcf2bd4e3afc0
"

build() {
    unpack vapoursynth-mvtools-17250aa97961.tar
    cd vapoursynth-mvtools-17250aa97961
    meson_plugin
    plugin_install '*mvtools*.so*' Plugins/VS/mvtools libmvtools.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/mvtools
}
