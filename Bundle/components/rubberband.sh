# the time stretching and pitch shifting library of the rubberband filter of FFmpeg
version=4.0.0 # renovate: datasource=github-tags depName=breakfastquay/rubberband extractVersion=^v(?<version>.+)$
sources="
git https://github.com/breakfastquay/rubberband.git 1d95888bec3ae0a17c0c4af791810d5a63f6bc35
"

build() {
    unpack rubberband-1d95888bec3a.tar
    cd rubberband-1d95888bec3a
    meson_build -Ddefault_library=shared -Dfft=builtin -Dresampler=builtin -Djni=disabled -Dladspa=disabled \
        -Dvamp=disabled -Dcmdline=disabled -Dtests=disabled
    stage_license COPYING
}
