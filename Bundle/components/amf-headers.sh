# the AMF headers of FFmpeg, the AMD driver provides the runtime
version=1.5.3 # renovate: datasource=github-releases depName=GPUOpen-LibrariesAndSDKs/AMF extractVersion=^v(?<version>.+)$
sources="
https://github.com/GPUOpen-LibrariesAndSDKs/AMF/releases/download/v1.5.3/AMF-headers-v1.5.3.tar.gz 0f521ad98041f8001147cded4a660127f18b1c27a801a5cd691bda1fa48381be
"

build() {
    unpack AMF-headers-v1.5.3.tar.gz
    mkdir -p $DESTDIR$PREFIX/include
    cp -r amf-headers-v1.5.3/AMF $DESTDIR$PREFIX/include/
    awk '/^\/\//{print; next} {exit}' amf-headers-v1.5.3/AMF/core/Factory.h > LICENSE
    stage_license LICENSE
}
