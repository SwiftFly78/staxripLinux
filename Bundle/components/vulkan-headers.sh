# for the encoders of rigaya, the Vulkan headers of Ubuntu 22.04 lack the video queues, the Vulkan loader comes
# from the system
version=1.4.368 # renovate: datasource=github-tags depName=KhronosGroup/Vulkan-Headers extractVersion=^v(?<version>.+)$
sources="
git https://github.com/KhronosGroup/Vulkan-Headers.git 88487f687f3100ebe1d3559a8a34d971ee85b977
"

build() {
    unpack Vulkan-Headers-88487f687f31.tar
    cd Vulkan-Headers-88487f687f31
    cmake_build
    stage_license LICENSE.md
}
