version=r14 # renovate: datasource=github-tags depName=HomeOfAviSynthPlusEvolution/neo_FFT3D versioning=loose
packages="
FFT3D Neo dual
"
depends="avisynthplus vapoursynth"
sources="
git https://github.com/HomeOfAviSynthPlusEvolution/neo_FFT3D.git 9f0d6350b0fee780353b0e86c381c8e3210877a5
"

build() {
    unpack neo_FFT3D-9f0d6350b0fe.tar
    cd neo_FFT3D-9f0d6350b0fe
    # FFTW's runtime packages and the bundle only have the versioned name
    sed -i 's/"libfftw3f_threads.so"/"libfftw3f_threads.so.3"/' src/fftwlite.h
    # the version comes from git describe, no TBB for the parallel algorithms
    git_version_tag r14
    cmake_plugin -DENABLE_PAR=OFF
    plugin_install '*neo-fft3d*.so*' 'Plugins/Dual/FFT3D Neo' libneo-fft3d.so
    stage_license_files
}

bundle() {
    plugin_bundle 'Plugins/Dual/FFT3D Neo'
}
