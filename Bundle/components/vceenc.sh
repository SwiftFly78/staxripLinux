# built with the libraries of the bundle, its releases link static libraries whose source isn't published
version=9.22 # renovate: datasource=github-tags depName=rigaya/VCEEnc
packages="VCEEncC"
depends="ffmpeg libass libplacebo libdovi libhdr10plus vapoursynth avisynthplus vulkan-headers opencl-headers amf-headers"
sources="
git https://github.com/rigaya/VCEEnc.git 820b7df570e8f2f40ad07b1c88bf10ddd6503a2b VCEEnc-9.22.tar
"
# the AMF submodule also contains Windows builds of FFmpeg, the build uses the headers of the amf-headers component
skip_submodules=AMF

build() {
    unpack VCEEnc-9.22.tar
    cd VCEEnc-9.22
    mkdir -p AMF/amf/public
    cp -r $PREFIX/include/AMF AMF/amf/public/include
    meson_build -Denable_libplacebo=true -Denable_vmaf=disabled -Denable_libvship=disabled
    stage_license VCEEnc_license.txt
}

bundle() {
    apps_install Encoders/VCEEncC bin/vceencc
}
