# the dispatcher of the Intel QSV encoders of FFmpeg, it loads the runtime of the system
version=2.17.0
sources="
git https://github.com/intel/libvpl.git d77f9195cf495b937631607333288fd917ae8939
"

build() {
    unpack libvpl-d77f9195cf49.tar
    cd libvpl-d77f9195cf49
    cmake_build -DBUILD_SHARED_LIBS=ON -DBUILD_TESTS=OFF -DBUILD_EXAMPLES=OFF -DINSTALL_EXAMPLES=OFF
    stage_license LICENSE
}
