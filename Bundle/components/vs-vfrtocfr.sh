version=1.0-d00a374
packages="VFRToCFR vs"
depends="vapoursynth"
sources="
git https://github.com/Irrational-Encoding-Wizardry/Vapoursynth-VFRToCFR.git d00a3743db70b928f3b0529d54f21aeae93d9db8
"

build() {
    unpack Vapoursynth-VFRToCFR-d00a3743db70.tar
    cd Vapoursynth-VFRToCFR-d00a3743db70
    # no build files
    g++ $CXXFLAGS -std=c++17 -shared -fPIC $(pkg-config --cflags vapoursynth) VFRToCFR.cpp -o libVFRToCFRVapoursynth.so
    plugin_install libVFRToCFRVapoursynth.so Plugins/VS/VFRToCFR libVFRToCFRVapoursynth.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/VFRToCFR
}
