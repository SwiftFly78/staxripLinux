# for the encoders of rigaya, the OpenCL headers of Ubuntu 22.04 lack the extensions they use, the OpenCL loader
# comes from the system
version=2026.05.29 # renovate: datasource=github-tags depName=KhronosGroup/OpenCL-Headers extractVersion=^v(?<version>.+)$
sources="
git https://github.com/KhronosGroup/OpenCL-Headers.git 6fe718c31a45fe25151362a72ef041c3a1047cbd
"

build() {
    unpack OpenCL-Headers-6fe718c31a45.tar
    cd OpenCL-Headers-6fe718c31a45
    cmake_build -DBUILD_TESTING=OFF
    stage_license LICENSE
}
