# for the encoders of rigaya, the Vulkan headers of Ubuntu 22.04 lack the video queues, the Vulkan loader comes
# from the system
version=1.4.365
sources="
git https://github.com/KhronosGroup/Vulkan-Headers.git c46850864f4661461b0f6cb9922c058ffea4915e
"

build() {
    unpack Vulkan-Headers-c46850864f46.tar
    cd Vulkan-Headers-c46850864f46
    cmake_build
    stage_license LICENSE.md
}
