# built with the libraries of the bundle, its releases link static libraries whose source isn't published
version=8.37 # renovate: datasource=github-tags depName=rigaya/QSVEnc
packages="QSVEncC"
depends="ffmpeg libass libplacebo libdovi libhdr10plus vapoursynth avisynthplus vulkan-headers opencl-headers"
sources="
git https://github.com/rigaya/QSVEnc.git 36685eec895b02ae99fe28a7374c47c363ba91d9
"

build() {
    unpack QSVEnc-36685eec895b.tar
    cd QSVEnc-36685eec895b
    # the setup builds the bundled oneVPL into the build folder, with DESTDIR its install would end up there
    env -u DESTDIR meson setup build --prefix=$PREFIX --libdir=lib --buildtype=release -Denable_vulkan=true \
        -Denable_libplacebo=true -Denable_vmaf=disabled -Denable_libvship=disabled -Denable_openvino=false
    ninja -C build
    ninja -C build install
    stage_license license.txt
}

bundle() {
    apps_install Encoders/QSVEncC bin/qsvencc
}
