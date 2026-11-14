# [Documentation](../../README.md) / [Tools](../README.md) / [Supported Encoders](README.md) / Video Encoders

| Profile | Encoder | Codec | Hardware | Chunks | HDR10+ / Dolby Vision | Full AppImage |
|---|---|---|---|---|---|---|
| x264 | [x264](https://www.videolan.org/developers/x264.html) | H.264 | | ✓ | | ✓ |
| x265 | [x265](https://code.videolan.org/videolan/x265) | HEVC | | ✓ | ✓ | ✓ |
| SvtAv1EncApp \| SVT-AV1 | [SVT-AV1](https://gitlab.com/AOMediaCodec/SVT-AV1) | AV1 | | ✓ | ✓ | ✓ |
| SvtAv1EncApp \| SVT-AV1-Essential | [SVT-AV1-Essential](https://github.com/nekotrix/SVT-AV1-Essential) | AV1 | | ✓ | ✓ | ✓ |
| SvtAv1EncApp \| SVT-AV1-HDR | [SVT-AV1-HDR](https://github.com/juliobbv-p/svt-av1-hdr) | AV1 | | ✓ | ✓ | ✓ |
| SvtAv1EncApp \| SVT-AV1-PSYEX | [SVT-AV1-PSYEX](https://github.com/BlueSwordM/svt-av1-psyex) | AV1 | | ✓ | ✓ | ✓ |
| SvtAv1EncApp \| SVT-AV1-Tritium | [SVT-AV1-Tritium](https://github.com/Uranite/svt-av1-tritium) | AV1 | | ✓ | ✓ | ✓ |
| AOMEnc | [aomenc](https://aomedia.googlesource.com/aom/) | AV1 | | ✓ | | ✓ |
| Rav1e | [rav1e](https://github.com/xiph/rav1e) | AV1 | | | | ✓ |
| vvencFFapp | [vvenc](https://github.com/fraunhoferhhi/vvenc) | VVC | | ✓ | | ✓ |
| NVEncC (Nvidia) \| H.264, H.265, AV1 | [NVEncC](https://github.com/rigaya/NVEnc) | H.264, HEVC, AV1 | NVIDIA | | ✓ | |
| QSVEncC (Intel) \| H.264, H.265, MPEG-2, VP9, AV1 | [QSVEncC](https://github.com/rigaya/QSVEnc) | H.264, HEVC, MPEG-2, VP9, AV1 | Intel | | ✓ | ✓ |
| VCEEncC (AMD) \| H.264, H.265, AV1 | [VCEEncC](https://github.com/rigaya/VCEEnc) | H.264, HEVC, AV1 | AMD | | ✓ | ✓ |
| ffmpeg \| ... | [FFmpeg](https://ffmpeg.org) | see below | | | | ✓ |
| Command Line | any encoder with a command line | | | | | |
| Copy/Mux | copies the video stream without encoding | | | | | |

Chunks: the video can be split into chunks that are encoded in parallel. HDR10+ / Dolby Vision: the dynamic HDR
metadata of the source is passed to the encoder, HDR10 works with all encoders that support 10-bit and the color
metadata. SvtAv1EncApp needs a build with Dolby Vision and HDR10+ support, see
[Tools](../../Introduction/Linux.md#tools).
NVEncC isn't part of the full AppImage, VCEEncC needs AMD's AMF runtime.

## FFmpeg profiles

| Profile | Codec |
|---|---|
| ffmpeg \| x264, x265, AOM-AV1 | H.264, HEVC and AV1 with the encoders built into FFmpeg |
| ffmpeg \| XviD, MPEG-4 | MPEG-4 Part 2 |
| ffmpeg \| Theora | Theora |
| ffmpeg \| VP \| VP8, VP9 | VP8 and VP9 (libvpx) |
| ffmpeg \| ProRes, ProRes-KS | Apple ProRes, for editing |
| ffmpeg \| R210, V210 | Uncompressed 10-bit RGB and YUV |
| ffmpeg \| UT Video, FFV1 | Lossless, UT Video with the prediction, the pixel format (YUV or RGB with alpha) and slices |
| ffmpeg \| GoPro CineForm HD | CineForm, for editing |
| ffmpeg \| Nvidia \| Nvidia H.264, H.265, AV1 | NVENC of NVIDIA GPUs |
| ffmpeg \| Intel \| Intel H.264, H.265, AV1 | Quick Sync of Intel GPUs |
| ffmpeg \| AMD \| AMD AMF H.264, H.265, AV1 | AMF of AMD GPUs, needs AMD's AMF runtime |
| ffmpeg \| VAAPI \| H.264, H.265, AV1 | VA-API of AMD and Intel GPUs with the open source drivers |
| ffmpeg \| Image \| PNG, JPEG, TIFF, BMP | Image sequences, an image per frame |

The FFmpeg of the full AppImage contains the encoders of these profiles. For the VA-API profiles, the device is
`/dev/dri/renderD128` unless another render node is set in the encoder options.
