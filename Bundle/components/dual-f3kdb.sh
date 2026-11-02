# the maintained fork of flash3kyuu_deband, AviSynth+ and VapourSynth in one library
version=2.0.1
packages="
flash3kyuu_deband dual
"
depends="avisynthplus vapoursynth"
sources="
git https://github.com/sauruuwu/flash3kyuu_deband.git 652974d8b0d591bd7f69cc8f75410260b9fe019d
"

build() {
    unpack flash3kyuu_deband-652974d8b0d5.tar
    cd flash3kyuu_deband-652974d8b0d5
    python3 waf configure --prefix=$PREFIX --enable-vapoursynth --enable-avisynth
    python3 waf build
    plugin_install 'libf3kdb*.so*' Plugins/Dual/flash3kyuu_deband libf3kdb.so
    stage_license license.txt
}

bundle() {
    plugin_bundle Plugins/Dual/flash3kyuu_deband
}
