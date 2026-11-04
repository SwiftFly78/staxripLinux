version=r27 # renovate: datasource=gitea-tags depName=StvG/avsresize registryUrl=https://codeberg.org versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$ source=1
zimg_version=release-3.0.6-f6cc75a # renovate: datasource=git-refs depName=https://github.com/sekrit-twc/zimg.git branch=master digest=f6cc75ad23db1bb9c53673c15523e6b6e960ffc6 source=2
packages="
AvsResize avs
"
depends="avisynthplus"
sources="
git https://codeberg.org/StvG/avsresize.git 825b7d6ad2b953c0cc02d13b01dd3af96901ede1
git https://github.com/sekrit-twc/zimg.git f6cc75ad23db1bb9c53673c15523e6b6e960ffc6
"

build() {
    unpack avsresize-825b7d6ad2b9.tar
    unpack zimg-f6cc75ad23db.tar
    # the zimg commit the build would download, with the patch the build applies
    (cd zimg-f6cc75ad23db && git apply --ignore-whitespace --whitespace=nowarn \
        ../avsresize-825b7d6ad2b9/cmake/zimg/zimg_patch.diff)
    cd avsresize-825b7d6ad2b9
    avs_export avsresize/avsresize.cpp
    cmake_plugin -DFETCHCONTENT_SOURCE_DIR_ZIMG=$PWD/../zimg-f6cc75ad23db
    plugin_install 'avsresize*.so*' Plugins/AVS/AvsResize libavsresize.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/AvsResize
}
