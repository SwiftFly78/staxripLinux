# the player of the preview and the play commands, for X11, Wayland desktops run it with XWayland, the Wayland
# and Vulkan versions of Ubuntu 22.04 are too old for it
version=0.41.0
packages=mpv
depends="ffmpeg libass libplacebo"
sources="
git https://github.com/mpv-player/mpv.git 41f6a645068483470267271e1d09966ca3b9f413
"

build() {
    unpack mpv-41f6a6450684.tar
    cd mpv-41f6a6450684
    meson_build --auto-features=disabled -Dlibmpv=false -Dbuild-date=false -Dlua=luajit -Diconv=enabled \
        -Djpeg=enabled -Dlcms2=enabled -Dlibavdevice=enabled -Dzlib=enabled -Dx11=enabled -Degl=enabled \
        -Degl-x11=enabled -Dgl=enabled -Dplain-gl=enabled -Dvaapi=enabled -Dvaapi-x11=enabled \
        -Dalsa=enabled -Dpulse=enabled
    stage_license LICENSE.GPL Copyright
}

bundle() {
    apps_install Support/mpv.net bin/mpv
}
