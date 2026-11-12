# [Documentation](../../README.md) / [Guides](../README.md) / [Encoder related](README.md) / Video

## Choosing an encoder

| Goal | Encoder |
|---|---|
| Best compatibility | x264 (H.264) |
| Good quality and size, HDR | x265 (HEVC) |
| Smallest files at high quality | SvtAv1EncApp (AV1), or one of its forks |
| Fast encoding with the GPU | NVEncC, QSVEncC, VCEEncC or the FFmpeg hardware profiles |
| Lossless or intermediate files | FFmpeg profiles like FFV1, UT Video or ProRes |

Software encoders (x264, x265, SVT-AV1, aomenc, rav1e, vvenc) give the best quality per bitrate. Hardware encoders of
the GPU are many times faster, but need more bitrate for the same quality, which makes them a good choice for
recordings and quick encodes.

## x264

- **Quality**: CRF `18` to `23`, lower values for better quality.
- **Preset**: `slow` or `slower`, `veryslow` gains little more.
- **Tune**: `film`, `animation` or `grain` for such content.
- 8-bit output plays everywhere.

## x265

- **Quality**: CRF `18` to `24` for 1080p, 4K sources can use slightly higher values.
- **Preset**: `slow` is a good balance, `medium` is about twice as fast.
- **Depth**: 10-bit, which is required for HDR.
- **HDR**: HDR10 is taken over with *Import VUI metadata*, HDR10+ and Dolby Vision with *Extract HDR metadata*
  ([Project Options](../../Usage/User-Interface/Project-Options.md#video)). For Dolby Vision set the *DV Profile*,
  usually
  `8.1`, on the Video card.
- **Chunks** speed up encoding on CPUs with many cores.

## SVT-AV1 and its forks

SvtAv1EncApp is the fastest AV1 software encoder with very good efficiency. Its forks SVT-AV1-Essential, SVT-AV1-HDR,
SVT-AV1-PSYEX and SVT-AV1-Tritium add perceptual tuning and options of their own, their websites in the
[Apps Manager](../../Usage/User-Interface/Apps-Manager.md) describe the differences. Each of them has its own encoder
profile.

Typical settings: CRF `25` to `35`, preset `4` to `6` (lower is slower and better), 10-bit. Film grain synthesis
(*film-grain*) keeps the look of grainy sources at a low bitrate. The full AppImage contains all forks, builds of the
system are only used if their version output names the fork, see [Tools](../../Introduction/Linux.md#tools).

## aomenc, rav1e and vvenc

- **aomenc** is the reference AV1 encoder, very efficient but much slower than SVT-AV1.
- **rav1e** is an AV1 encoder written in Rust.
- **vvenc** encodes VVC (H.266), the successor of HEVC. Few players support VVC yet, StaxRipLinux muxes it into MP4.

## Hardware encoders

- **NVEncC** (NVIDIA), **QSVEncC** (Intel) and **VCEEncC** (AMD) encode H.264, HEVC and AV1 on the GPU and support
  HDR10, HDR10+ and Dolby Vision. NVEncC isn't part of the full AppImage, see the [Apps
  Manager](../../Usage/User-Interface/Apps-Manager.md)
  for how to install it. VCEEncC needs AMD's AMF runtime.
- The **FFmpeg** profiles `ffmpeg | Nvidia`, `ffmpeg | Intel`, `ffmpeg | AMD` and `ffmpeg | VAAPI` use the hardware
  encoders through FFmpeg. VA-API works with the open source drivers of AMD and Intel GPUs without additional
  runtimes.

Quality based modes (CQP, CQ, ICQ) are usually the best choice. Hardware encoders need noticeably more bitrate than
x265 for the same quality, slower presets of the hardware encoders close some of the gap.

## Testing settings

1. Cut a representative part of one to two minutes in the [Preview](../../Usage/User-Interface/Preview.md), for example
   a dark scene and a scene with much motion.
2. Encode it with different settings, save each encode with another target name.
3. Compare the encodes with the source in the [Video Comparison](../../Usage/User-Interface/Video-Comparison.md).
