version=R2 # renovate: datasource=github-tags depName=myrsloik/VapourSynth-FFT3DFilter versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
FFT3DFilter vs
"
depends="vapoursynth"
sources="
git https://github.com/myrsloik/VapourSynth-FFT3DFilter.git 50194b45430abbb5bcbd8e8d016d81c83be8a599
"

build() {
    unpack VapourSynth-FFT3DFilter-50194b45430a.tar
    cd VapourSynth-FFT3DFilter-50194b45430a
    meson_plugin
    plugin_install '*fft3dfilter*.so*' Plugins/VS/FFT3DFilter libfft3dfilter.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/FFT3DFilter
}
