version=0.4.41
sources="
https://mediaarea.net/download/source/libzen/0.4.41/libzen_0.4.41.tar.bz2 eb237d7d3dca6dc6ba068719420a27de0934a783ccaeb2867562b35af3901e2d
"

build() {
    unpack libzen_0.4.41.tar.bz2
    cd ZenLib/Project/CMake
    cmake_build -DBUILD_SHARED_LIBS=ON
    stage_license ../../License.txt
}
