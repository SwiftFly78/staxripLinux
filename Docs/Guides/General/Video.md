# [Documentation](../../README.md) / [Guides](../README.md) / [General](README.md) / Video

## Containers and codecs

A video file is a **container**, like MKV, MP4 or TS, which holds **streams**: usually one video stream, audio
streams, subtitles and chapters. The **codec** is the format of a stream, like H.264, HEVC (H.265) or AV1 for video.
An **encoder** creates a stream of a codec, for example x265 creates HEVC.

StaxRipLinux encodes the video, processes the audio tracks and muxes everything into the target container, so the
codec is chosen on the [Video](../../Usage/User-Interface/Main.md#video) card and the container on the
[Target](../../Usage/User-Interface/Main.md#target) card. MKV holds almost everything, MP4 is supported by more
devices, but not every audio and subtitle codec can be put into it.

| Codec | Encoders | Notes |
|---|---|---|
| H.264 (AVC) | x264, NVEncC, QSVEncC, VCEEncC, FFmpeg | Plays everywhere, needs the most bitrate of the modern codecs |
| HEVC (H.265) | x265, NVEncC, QSVEncC, VCEEncC, FFmpeg | About 30 to 50% smaller than H.264 at the same quality, standard for 4K and HDR |
| AV1 | SVT-AV1, aomenc, rav1e, NVEncC, QSVEncC, VCEEncC, FFmpeg | Royalty free, more efficient than HEVC, needs newer devices |
| VVC (H.266) | vvenc | The newest codec, few players support it |

## Resolution and aspect ratio

The resolution is the size in pixels, like 1920×1080. The **display aspect ratio** (DAR) is the shape of the picture
on the screen, like 16:9. Usually the pixels are square, so the DAR is width / height. Some videos, like DVDs, have
non-square pixels: a PAL DVD stores 720×576 pixels and is shown as 16:9 or 4:3. The **pixel aspect ratio** (PAR, also
called sample aspect ratio) describes the shape of a pixel, the **storage aspect ratio** (SAR) is width / height of the
stored pixels: DAR = SAR × PAR.

The [Crop and Resize](../../Usage/User-Interface/Main.md#crop-and-resize) card shows all three. When a video is
cropped and resized, StaxRipLinux keeps the aspect ratio, the **error** shows how far the result differs from the
original shape. Sizes that are divisible by 2 (the output mod) are required for 4:2:0 color, some devices need 8 or 16.

**Cropping** removes black borders, which only waste bitrate, see [Cropping](../../Usage/Cropping.md).
**Resizing** makes the video smaller, for example 1080p to 720p for small screens. Upscaling doesn't add detail.

## Frame rate and interlacing

The **frame rate** is the number of frames per second: films have 24 (23.976), PAL TV 25, NTSC TV 29.97 or 59.94.
Most videos have a constant frame rate (CFR), some, like smartphone recordings, a variable one (VFR). StaxRipLinux
extracts the timestamps of VFR MKV files and passes them to the source filter and the muxer, so the timing is kept.

TV and DVDs are often **interlaced**: each frame consists of two fields from different moments, which shows combing
lines on motion. Interlaced video should be **deinterlaced** with a filter like QTGMC or BWDIF in the `Field` category.
Films on NTSC DVDs are often **telecined** (24 frames turned into 30 by repeating fields), which is reversed with an
IVTC filter instead of deinterlacing. The Source card shows the scan type of the source, the
[Preview](../../Usage/User-Interface/Preview.md) shows combing.

## Color and HDR

- **Bit depth**: 8-bit has 256 shades per channel, 10-bit 1024. 10-bit encodes show less banding and often even
  compress slightly better, so HEVC and AV1 are usually encoded in 10-bit, the *Depth* option of the encoder sets it.
  H.264 in 10-bit is badly supported by players.
- **Chroma subsampling**: 4:2:0 stores the color with half the resolution in both directions, which almost all videos
  use. 4:2:2 and 4:4:4 are used for production.
- **Color metadata**: the matrix, primaries, transfer and range describe how the colors are meant to be shown, like
  BT.709 for HD and BT.2020 for UHD. *Import VUI metadata* passes them to the encoder.
- **HDR**: HDR10 adds static metadata (mastering display, MaxCLL, MaxFALL) to 10-bit BT.2020 video with the PQ
  transfer. **HDR10+** and **Dolby Vision** add dynamic metadata for every scene or frame. StaxRipLinux keeps HDR10
  with *Import VUI metadata* and HDR10+ and Dolby Vision with *Extract HDR metadata*, see
  [Encoder Options](../../Usage/User-Interface/Encoder-Options.md#hdr). HDR needs an encoder that supports 10-bit and
  the metadata, like x265 or SVT-AV1.

## Rate control

The rate control decides how many bits each frame gets:

- **Constant quality** (CRF in x264, x265 and SVT-AV1, CQ in hardware encoders): every frame gets the bits it needs for
  the same visual quality, so the file size depends on the content. This is the best mode for most encodes.
  The value is the quality, lower is better: x264 and x265 are typically used from 16 to 24, SVT-AV1 from 20 to 35.
- **Constant quantizer** (CQP): every frame is compressed with the same quantizer, simple but less efficient.
- **Bitrate** (ABR, VBR) and **2-pass**: the encoder aims for an average bitrate, so the file size is known in advance.
  2-pass analyzes the video first and distributes the bits better than 1-pass. Use it when the file must have a
  certain size, the [Target](../../Usage/User-Interface/Main.md#target) card calculates the bitrate from the size.

The **preset** trades encoding speed for efficiency: slower presets give the same quality with a smaller file. The
**tune** optimizes for a kind of content, like `Animation` or `Grain`.

> [!TIP]
> Encode a short part with a few quality values and compare them with the source in the
> [Video Comparison](../../Usage/User-Interface/Video-Comparison.md) to find the highest value that still looks good
> to you. The [Preview](../../Usage/User-Interface/Preview.md) can cut a test section.
