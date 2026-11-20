# the player of the preview and the play commands, for X11, Wayland desktops run it with XWayland, the Wayland
# and Vulkan versions of Ubuntu 22.04 are too old for it
version=0.42.0 # renovate: datasource=github-tags depName=mpv-player/mpv extractVersion=^v(?<version>.+)$
packages=mpv
depends="ffmpeg libass libplacebo"
sources="
git https://github.com/mpv-player/mpv.git b0841e8c4d156b8403dc1876c4cbd9011eb583f3
"

build() {
    unpack mpv-b0841e8c4d15.tar
    cd mpv-b0841e8c4d15
    meson_build --auto-features=disabled -Dlibmpv=false -Dbuild-date=false -Dlua=luajit -Diconv=enabled \
        -Djpeg=enabled -Dlcms2=enabled -Dlibavdevice=enabled -Dzlib=enabled -Dx11=enabled -Degl=enabled \
        -Degl-x11=enabled -Dgl=enabled -Dplain-gl=enabled -Dvaapi=enabled -Dvaapi-x11=enabled \
        -Dalsa=enabled -Dpulse=enabled
    stage_license LICENSE.GPL Copyright
}

bundle() {
    apps_install Support/mpv.net bin/mpv
}
