# built with the libraries of the bundle, its releases link static libraries whose source isn't published
version=9.22
packages="VCEEncC"
depends="ffmpeg libass libplacebo libdovi libhdr10plus vapoursynth avisynthplus vulkan-headers opencl-headers"
sources="
git https://github.com/rigaya/VCEEnc.git 820b7df570e8f2f40ad07b1c88bf10ddd6503a2b
"

build() {
    unpack VCEEnc-820b7df570e8.tar
    cd VCEEnc-820b7df570e8
    meson_build -Denable_libplacebo=true -Denable_vmaf=disabled -Denable_libvship=disabled
    stage_license VCEEnc_license.txt
}

bundle() {
    apps_install Encoders/VCEEncC bin/vceencc
}
