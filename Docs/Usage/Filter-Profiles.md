# [Documentation](../README.md) / [Usage](README.md) / Filter Profiles

Filter profiles define the filters of the filter menus: their category, name and code. AviSynth+ and VapourSynth have
their own profiles, they're edited with `Profiles` in the menu of the [filters](User-Interface/Main.md#filters), see
[Filter Profiles](User-Interface/Filter-Profiles.md) in the user interface section.

## Format

```
[Category]
Name = code
Group | Name = code

Name with several lines =
    first line
    second line
```

- `[Category]` starts a category, like `[Noise]`. The category of a filter decides where it's placed in the script and
  which filter it replaces: a script has one filter per category, unless it's added with *Add* or *Insert*.
- `Name = code` defines a filter, the code is AviSynth+ or VapourSynth code.
- `Group | Name` puts the filter into a submenu, for example `DFTTest | Strong`.
- Code with several lines starts in the next line, each line indented by four spaces or a tab.

The `Source`, `Crop` and `Resize` categories are required. The source filters of `[Source]` are also used by the
[Source Filters](User-Interface/Settings.md#source-filters) settings, which assign them to file formats.

## Macros

The code may contain [macros](Macros.md), which are replaced when the script is generated, for example:

- `%source_file%`: the source file
- `%source_temp_file%`: the source file in the temp folder, without extension, for index and cache files
- `%target_width%`, `%target_height%`: the size of the [Crop and Resize](User-Interface/Main.md#crop-and-resize) card
- `%crop_left%`, `%crop_top%`, `%crop_right%`, `%crop_bottom%`: the crop values

Interactive macros ask when the filter is added:

| Macro | Description |
|---|---|
| `$select:item1;item2;...$` | Selection from a list. `msg:text` as first item shows a message, `caption\|value` shows the caption and inserts the value |
| `$enter_text$`, `$enter_text:prompt$` | Text entered in an input box |
| `$browse_file$` | A file chosen with a file dialog |

## Examples

A VapourSynth filter that asks for the strength:

```
[Sharpen]
CAS = clip = core.cas.CAS(clip, sharpness=$select:msg:Select the strength;Light|0.3;Medium|0.5;Strong|0.8$)
```

An AviSynth+ filter with several lines:

```
[Noise]
MCTemporalDenoise | Custom =
    ConvertBits(16)
    MCTemporalDenoise(settings="medium", stabilize=true)
    ConvertBits(10, dither=1)
```

A source filter for VapourSynth that uses BestSource with a cache in the temp folder:

```
[Source]
BestSource Custom = clip = core.bs.VideoSource(r"%source_file%", cachepath=r"%source_temp_file%")
```

> [!TIP]
> Filters that are only needed for one project don't need a profile, the [Code Editor](User-Interface/Code-Editor.md)
> can change the code of any filter. The *Empty Filter* entry of *Add* adds a filter without code.

## Plugins

StaxRipLinux detects the plugins a filter uses by the names of its functions and adds the code that loads them, if
*Load AviSynth plugins* and *Load VapourSynth plugins* are enabled in the
[Settings](User-Interface/Settings.md#frameserver).
Plugins that StaxRipLinux doesn't know have to be loaded in the code, for example with
`LoadPlugin("/path/to/libplugin.so")` (AviSynth+) or `core.std.LoadPlugin("/path/to/libplugin.so")` (VapourSynth), or
installed in a folder the frame server loads automatically, see
[How StaxRipLinux finds tools and plugins](../Introduction/Linux.md#how-staxriplinux-finds-tools-and-plugins).
