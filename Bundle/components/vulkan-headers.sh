# for the encoders of rigaya, the Vulkan headers of Ubuntu 22.04 lack the video queues, the Vulkan loader comes
# from the system
version=1.4.366 # renovate: datasource=github-tags depName=KhronosGroup/Vulkan-Headers extractVersion=^v(?<version>.+)$
sources="
git https://github.com/KhronosGroup/Vulkan-Headers.git 73814d006638aa00bd553215b054f0f02e457e22
"

build() {
    unpack Vulkan-Headers-73814d006638.tar
    cd Vulkan-Headers-73814d006638
    cmake_build
    stage_license LICENSE.md
}
