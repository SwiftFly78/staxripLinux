# the NVENC and NVDEC headers of FFmpeg, the NVIDIA driver provides the libraries, needs driver 550 or newer
version=12.2.72.0
sources="
git https://github.com/FFmpeg/nv-codec-headers.git c69278340ab1d5559c7d7bf0edf615dc33ddbba7
"

build() {
    unpack nv-codec-headers-c69278340ab1.tar
    cd nv-codec-headers-c69278340ab1
    make PREFIX=$PREFIX install
    sed -n '1,/\*\//p' include/ffnvcodec/nvEncodeAPI.h > LICENSE
    stage_license LICENSE
}
