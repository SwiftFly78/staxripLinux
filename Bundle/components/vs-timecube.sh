version=r5
packages="
TimeCube vs
"
depends="vapoursynth"
sources="
git https://github.com/sekrit-twc/timecube.git 02713a6e22ef8c88d15ea4e88f2016f8d0c8cc13
"

build() {
    unpack timecube-02713a6e22ef.tar
    cd timecube-02713a6e22ef
    # GCC 12 needs <algorithm>, which the source doesn't include
    CXXFLAGS="$CXXFLAGS -include algorithm" make -j$JOBS
    plugin_install 'vscube*.so*' Plugins/VS/TimeCube libvscube.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/TimeCube
}
