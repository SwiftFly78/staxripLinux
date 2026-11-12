# [Documentation](../../README.md) / [Guides](../README.md) / [Frame Server related](README.md) / VapourSynth

[VapourSynth](https://www.vapoursynth.com) is a frame server based on Python. A script creates a clip, changes it with
the functions of plugins and Python modules and sets the result as output:

```python
import vapoursynth as vs
core = vs.core

clip = core.ffms2.Source(r"/home/user/Videos/Sample.mkv")
clip = core.std.Crop(clip, top=140, bottom=140)
clip = core.resize.Spline36(clip, 1280, 534)
clip.set_output()
```

Plugin functions are called with `core.<namespace>.<function>`, like `core.dfttest.DFTTest`. Python modules like
havsfunc provide functions that combine several plugins, like QTGMC.

## Using VapourSynth in StaxRipLinux

The templates of StaxRipLinux use VapourSynth. Every filter of the [Filters](../../Usage/User-Interface/Main.md#filters)
card is a piece of Python code that works on the variable `clip`, StaxRipLinux adds the imports and the output. The
filters of the menu come from the VapourSynth [filter profiles](../../Usage/Filter-Profiles.md), the
[Code Editor](../../Usage/User-Interface/Code-Editor.md) changes their code.

The full AppImage contains VapourSynth R79 with its own Python, the plugins and the Python modules of the filter
profiles, like havsfunc, mvsfunc, muvsfunc, G41Fun and vsTAAmbk. Without it, VapourSynth and the plugins come from the
system, see [Requirements](../../Introduction/Linux.md#requirements).

VapourSynth scripts are passed to the encoders with vspipe.

## Plugins and Python modules

- VapourSynth loads the plugins of its plugin folders automatically, StaxRipLinux loads the plugins it knows with
  `core.std.LoadPlugin` if they aren't loaded yet.
- Other plugins can be placed in `Plugins/VapourSynth` of the settings folder or `~/.local/lib/vapoursynth`, see
  [How StaxRipLinux finds tools and plugins](../../Introduction/Linux.md#how-staxriplinux-finds-tools-and-plugins).
- Python modules are imported in the code, like `import havsfunc`. With the VapourSynth of the system, modules are
  installed for its Python, for example with pip. The VapourSynth of the full AppImage has its own Python, modules of
  the system Python aren't available to it.

Recent VapourSynth versions need a configuration that tells them which Python to use, it's usually created
automatically.
If StaxRipLinux reports that VapourSynth can't find Python, run `vapoursynth config`.

## Common tasks

Deinterlacing with QTGMC:

```python
import havsfunc
clip = havsfunc.QTGMC(clip, TFF=True, Preset='Slower')
```

Denoising with DFTTest:

```python
clip = core.dfttest.DFTTest(clip, sigma=4, tbsize=3)
```

Debanding with neo_f3kdb:

```python
clip = core.neo_f3kdb.Deband(clip, range=15, y=48, cb=32, cr=32, grainy=0, grainc=0)
```

Processing in high bit depth:

```python
clip = core.fmtc.bitdepth(clip, bits=16)
clip = core.neo_f3kdb.Deband(clip, y=48, cb=32, cr=32, output_depth=16)
clip = core.fmtc.bitdepth(clip, bits=10, dmode=3)
```

## Testing scripts

- The [Preview](../../Usage/User-Interface/Preview.md) shows the output, `R` reloads it after a change.
- `Info...` in the [Code Editor](../../Usage/User-Interface/Code-Editor.md) shows the size, frame rate and format,
  `Advanced Info...` benchmarks the script with vspipe.
- Errors show the Python traceback with the line that caused it. The script is saved in the temp folder of the
  project and can be tested in a terminal with `vspipe --info script.vpy`.
