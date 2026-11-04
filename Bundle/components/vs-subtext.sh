version=R7 # renovate: datasource=github-tags depName=vapoursynth/subtext versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
SubText vs
"
depends="vapoursynth libass ffmpeg"
sources="
git https://github.com/vapoursynth/subtext.git 662577dda970b80587985fbab72234eb8b5649d7
"

build() {
    unpack subtext-662577dda970.tar
    cd subtext-662577dda970
    meson_plugin
    plugin_install '*subtext*.so*' Plugins/VS/SubText libSubText.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/SubText
}
