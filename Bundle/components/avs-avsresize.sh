version=r27 # renovate: datasource=gitea-tags depName=StvG/avsresize registryUrl=https://codeberg.org versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$ source=1
zimg_version=release-3.0.6-9806db4 # renovate: datasource=git-refs depName=https://github.com/sekrit-twc/zimg.git branch=master digest=9806db428022893543121c94c7069263cd92a71a source=2
packages="
AvsResize avs
"
depends="avisynthplus"
sources="
git https://codeberg.org/StvG/avsresize.git 825b7d6ad2b953c0cc02d13b01dd3af96901ede1
git https://github.com/sekrit-twc/zimg.git 9806db428022893543121c94c7069263cd92a71a
"

build() {
    unpack avsresize-825b7d6ad2b9.tar
    unpack zimg-9806db428022.tar
    # the zimg commit the build would download, with the patch the build applies
    (cd zimg-9806db428022 && git apply --ignore-whitespace --whitespace=nowarn \
        ../avsresize-825b7d6ad2b9/cmake/zimg/zimg_patch.diff)
    cd avsresize-825b7d6ad2b9
    avs_export avsresize/avsresize.cpp
    cmake_plugin -DFETCHCONTENT_SOURCE_DIR_ZIMG=$PWD/../zimg-9806db428022
    plugin_install 'avsresize*.so*' Plugins/AVS/AvsResize libavsresize.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/AvsResize
}
