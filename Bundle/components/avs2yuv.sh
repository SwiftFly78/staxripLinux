version=0.30-80ce55d # renovate: datasource=git-refs depName=https://github.com/DJATOM/avs2yuv.git branch=master digest=80ce55dffba41b1e3ae71f90c8628a7475150e87
packages=avs2yuv
depends="avisynthplus"
sources="
git https://github.com/DJATOM/avs2yuv.git 80ce55dffba41b1e3ae71f90c8628a7475150e87
"

build() {
    unpack avs2yuv-80ce55dffba4.tar
    cd avs2yuv-80ce55dffba4
    cmake_build
    stage_license gpl.txt
}

bundle() {
    apps_install FrameServer/AviSynth bin/avs2yuv
    # it loads libavisynth.so of its folder
    patchelf --set-rpath '$ORIGIN' $O/Apps/FrameServer/AviSynth/avs2yuv
}
