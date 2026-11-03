version=2.0-b023e21 # renovate: datasource=git-refs depName=https://github.com/VFR-maniac/VapourSynth-FFT3DFilter.git branch=master digest=b023e21954423f29fdefcb54a6b0540deb3bdac4
packages="
FFT3DFilter vs
"
depends="vapoursynth"
sources="
git https://github.com/VFR-maniac/VapourSynth-FFT3DFilter.git b023e21954423f29fdefcb54a6b0540deb3bdac4
"

build() {
    unpack VapourSynth-FFT3DFilter-b023e2195442.tar
    cd VapourSynth-FFT3DFilter-b023e2195442
    # the makefile runs configure without a path
    chmod +x configure
    # GCC 12 needs <cmath>, which the source doesn't include
    ./configure --extra-cxxflags="-include cmath"
    make -j$JOBS
    plugin_install '*fft3dfilter*.so*' Plugins/VS/FFT3DFilter libfft3dfilter.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/FFT3DFilter
}
