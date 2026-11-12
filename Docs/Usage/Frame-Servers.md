# [Documentation](../README.md) / [Usage](README.md) / Frame Servers

A frame server is a scripting language for video processing. StaxRipLinux writes a script with the
[filters](User-Interface/Main.md#filters) of the project, the frame server runs it, and the encoder gets the frames it
returns. Every filter, from the source filter that decodes the video to crop, deinterlacing, denoising and resizing,
is a line of this script.

StaxRipLinux supports two frame servers:

- **[VapourSynth](https://www.vapoursynth.com)** is based on Python. Scripts are `.vpy` files, its plugins and Python
  scripts like havsfunc or mvsfunc cover almost every filter. The templates of StaxRipLinux use VapourSynth.
- **[AviSynth+](https://avs-plus.net)** has its own scripting language, scripts are `.avs` files. Many classic filters
  were written for it, for example QTGMC, and many of them were ported to Linux.

Both are equally supported. The button of the [Filters](User-Interface/Main.md#filters) card shows the current frame
server, `Filter Setup` in its menu switches to the default setup of the other one. The
[guides](../Guides/FrameServer/README.md) describe both in more detail.

## Scripts

A VapourSynth script that StaxRipLinux generates for a source with black borders, cropped and resized to 1280×534,
looks like this:

```python
import os, sys
import vapoursynth as vs
core = vs.core

tcFile = r"/home/user/Videos/Sample.mkv_temp/Sample_timestamps.txt" # timestamps file path
clip = core.ffms2.Source(r"/home/user/Videos/Sample.mkv", cachefile=r"/home/user/Videos/Sample.mkv_temp/temp.ffindex", timecodes=tcFile) if os.path.exists(tcFile) else core.ffms2.Source(r"/home/user/Videos/Sample.mkv", cachefile=r"/home/user/Videos/Sample.mkv_temp/temp.ffindex")
clip = core.std.Crop(clip, 0, 0, 140, 140)
clip = core.resize.Bicubic(clip, 1280, 534)

clip.set_output()
```

The same project with AviSynth+:

```
tcFile = "/home/user/Videos/Sample.mkv_temp/Sample_timestamps.txt" # timestamps file path
Exist(tcFile) ? FFVideoSource("/home/user/Videos/Sample.mkv", cachefile="/home/user/Videos/Sample.mkv_temp/temp.ffindex", timecodes=tcFile) : FFVideoSource("/home/user/Videos/Sample.mkv", cachefile="/home/user/Videos/Sample.mkv_temp/temp.ffindex")
Crop(0, 140, -0, -140)
BicubicResize(1280, 534)
```

The source filter uses the timestamps of the source if they were extracted, for variable frame rate videos.

`Preview Code` in the filter menu shows the complete script of the project, which also contains the lines that load
the plugins. The script is saved in the temp folder and passed to the encoder, usually piped by vspipe or avs2yuv.

## Source filters

The source filter opens the video. The *Automatic* source filter chooses one by the format of the source, see
[Source Filters](User-Interface/Settings.md#source-filters):

| Source filter | VapourSynth | AviSynth+ | Notes |
|---|---|---|---|
| FFMS2 | `ffms2.Source` | `FFVideoSource` | Works with almost every format, indexes the file first |
| L-SMASH Works | `lsmas.LWLibavSource`, `lsmas.LibavSMASHSource` | `LWLibavVideoSource`, `LSMASHVideoSource` | Good for transport streams (TS, M2TS) and MP4 |
| BestSource | `bs.VideoSource` | `BSVideoSource` | Frame accurate, slower indexing |
| D2V | `d2v.Source` | `D2VSource` | MPEG-2 sources indexed by D2V Witch |

The *Manual* source filter asks which one to use when a source is opened.

## Installed or portable

The full AppImage contains VapourSynth with its own Python and AviSynth+, with the plugins of the filter profiles.
The slim AppImage and builds from source use the frame servers installed on the system,
see [Requirements](../Introduction/Linux.md#requirements). `AviSynth Mode` and `VapourSynth Mode` in the
[Settings](User-Interface/Settings.md#frameserver) choose between the frame servers of the AppImage and the
installed ones.

Plugins are loaded automatically: StaxRipLinux detects the functions a script uses and adds the lines that load their
plugins. Plugins that aren't part of StaxRipLinux can be placed in `Plugins/VapourSynth` or `Plugins/AviSynth` in
the settings folder, see [How StaxRipLinux finds tools and
plugins](../Introduction/Linux.md#how-staxriplinux-finds-tools-and-plugins).

## Scripts as source

Existing `.vpy` and `.avs` scripts can be opened like a video, the script is then the source of the project.
