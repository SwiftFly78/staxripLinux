# for the encoders of rigaya, the Vulkan headers of Ubuntu 22.04 lack the video queues, the Vulkan loader comes
# from the system
version=1.4.367 # renovate: datasource=github-tags depName=KhronosGroup/Vulkan-Headers extractVersion=^v(?<version>.+)$
sources="
git https://github.com/KhronosGroup/Vulkan-Headers.git 9fbc2a782ee3f8a4db64e7d991e23da75e893f44
"

build() {
    unpack Vulkan-Headers-9fbc2a782ee3.tar
    cd Vulkan-Headers-9fbc2a782ee3
    cmake_build
    stage_license LICENSE.md
}
