# [Documentation](../../README.md) / [Guides](../README.md) / [Frame Server related](README.md) / AviSynth+

[AviSynth+](https://avs-plus.net) processes video with scripts in its own language. Each line calls a filter, which
works on the clip of the previous line:

```
FFVideoSource("/home/user/Videos/Sample.mkv")
Crop(0, 140, -0, -140)
QTGMC(Preset="Medium")
Spline36Resize(1280, 534)
```

## Using AviSynth+ in StaxRipLinux

`Filter Setup > AviSynth` in the menu of the [Filters](../../Usage/User-Interface/Main.md#filters) card switches the
project to AviSynth+. The filters of the menu come from the AviSynth+ [filter profiles](../../Usage/Filter-Profiles.md),
the [Code Editor](../../Usage/User-Interface/Code-Editor.md) changes their code. To use AviSynth+ for all projects,
switch a template and save it.

The full AppImage contains AviSynth+ with the plugins and scripts of the filter profiles, like QTGMC, SMDegrain,
MCTemporalDenoise, LSFmod, FineSharp and many others. Without it, AviSynth+ (`libavisynth.so`) and the plugins come
from the system, Arch Linux has `avisynthplus` in the official repositories and many plugins in the AUR.

AviSynth+ scripts are passed to the encoders with avs2yuv. Without avs2yuv FFmpeg is used, which must be built with
AviSynth support.

## Plugins

AviSynth+ plugins on Linux are shared libraries (`.so`), scripts with functions are `.avsi` files.

- StaxRipLinux loads the plugins of the filters it knows with `LoadPlugin` and the scripts with `Import`.
- AviSynth+ loads the plugins of its autoload folders itself, like `/usr/lib/avisynth` of an installation of the
  system.
- Other plugins can be placed in `Plugins/AviSynth` of the settings folder, see
  [How StaxRipLinux finds tools and plugins](../../Introduction/Linux.md#how-staxriplinux-finds-tools-and-plugins), or
  loaded explicitly in the code: `LoadPlugin("/path/to/libplugin.so")`.

Plugins must be built for Linux. StaxRipLinux knows the Linux file names of the plugins of its filter profiles, for
example `libmvtools2.so` for MVTools.

## Common tasks

Deinterlacing with QTGMC:

```
QTGMC(Preset="Slower")
```

Denoising with SMDegrain, temporal and motion compensated:

```
SMDegrain(tr=2, thSAD=300)
```

Debanding with neo_f3kdb:

```
neo_f3kdb(range=15, Y=48, Cb=32, Cr=32, grainY=0, grainC=0)
```

High bit depth processing, for example for debanding of 8-bit sources:

```
ConvertBits(16)
neo_f3kdb(Y=48, Cb=32, Cr=32, output_depth=16)
ConvertBits(10, dither=1)
```

## Testing scripts

- The [Preview](../../Usage/User-Interface/Preview.md) shows the output, `R` reloads it after a change.
- `Info...` in the [Code Editor](../../Usage/User-Interface/Code-Editor.md) shows the size, frame rate and color
  format, `Advanced Info...` runs avs2yuv to show the info and benchmark the script.
- Errors are shown with the line that caused them. The script itself is saved in the temp folder of the project, it
  can be tested in a terminal with `avs2yuv script.avs -o /dev/null`.
