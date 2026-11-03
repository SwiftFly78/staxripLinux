version=r7 # renovate: datasource=github-tags depName=HomeOfVapourSynthEvolution/VapourSynth-DFTTest versioning=loose
packages="
DFTTest vs
"
depends="vapoursynth"
sources="
git https://github.com/HomeOfVapourSynthEvolution/VapourSynth-DFTTest.git b22858824a938d910cbba2ff095d8f4217e9993f
"

build() {
    unpack VapourSynth-DFTTest-b22858824a93.tar
    cd VapourSynth-DFTTest-b22858824a93
    # Ubuntu's FFTW has the thread functions DFTTest calls in a separate library
    sed -i "s/^fftw3f_dep = dependency('fftw3f')$/fftw3f_dep = [dependency('fftw3f'), meson.get_compiler('cpp').find_library('fftw3f_threads')]/" \
        meson.build
    meson_plugin
    plugin_install '*dfttest*.so*' Plugins/VS/DFTTest libDFTTest.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/DFTTest
}
