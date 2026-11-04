version=r27 # renovate: datasource=gitea-tags depName=StvG/avsresize registryUrl=https://codeberg.org versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$ source=1
zimg_version=release-3.0.6-67e0603 # renovate: datasource=git-refs depName=https://github.com/sekrit-twc/zimg.git branch=master digest=67e0603271c080e22c8429856dd4a8a56587e61e source=2
packages="
AvsResize avs
"
depends="avisynthplus"
sources="
git https://codeberg.org/StvG/avsresize.git 825b7d6ad2b953c0cc02d13b01dd3af96901ede1
git https://github.com/sekrit-twc/zimg.git 67e0603271c080e22c8429856dd4a8a56587e61e
"

build() {
    unpack avsresize-825b7d6ad2b9.tar
    unpack zimg-67e0603271c0.tar
    # the zimg commit the build would download, with the patch the build applies
    (cd zimg-67e0603271c0 && git apply --ignore-whitespace --whitespace=nowarn \
        ../avsresize-825b7d6ad2b9/cmake/zimg/zimg_patch.diff)
    cd avsresize-825b7d6ad2b9
    avs_export avsresize/avsresize.cpp
    cmake_plugin -DFETCHCONTENT_SOURCE_DIR_ZIMG=$PWD/../zimg-67e0603271c0
    plugin_install 'avsresize*.so*' Plugins/AVS/AvsResize libavsresize.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/AvsResize
}
