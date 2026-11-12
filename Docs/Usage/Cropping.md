# [Documentation](../README.md) / [Usage](README.md) / Cropping

Many videos have black borders, for example a 2.40:1 movie on a 16:9 Blu-ray. Encoding the borders wastes bitrate and
their sharp edges cost quality, so they are usually cropped. Players add borders again as needed.

The crop values are the number of pixels removed from the left, top, right and bottom. The
[Crop and Resize](User-Interface/Main.md#crop-and-resize) card shows them, the [Crop](User-Interface/Crop.md) window
shows them on the video and lets you change them. The `Crop` filter in the [filters](User-Interface/Main.md#filters)
applies them, it should come right after the source filter.

## Auto Crop

Auto Crop analyzes frames spread over the video and finds the borders whose lines aren't brighter than the
*Luminance Threshold*. It runs inside StaxRipLinux and is started:

- after opening a source, if *Auto Crop after opening* is set to `Always`, or `Dolby Vision only` for Dolby Vision
  sources, in the [Project Options](User-Interface/Project-Options.md#auto-crop),
- with `Auto Crop` in the [Crop](User-Interface/Crop.md) window (`A`), or with the command `StartAutoCrop` in a
  custom menu or an [event command](Events.md).

Check the result in the crop window, especially for videos with dark scenes, logos or changing aspect ratios. The
Auto Crop options decide which sides are cropped, how many and which frames are analyzed and how many frames at the
start and end are ignored, for example the credits.

## Smart Crop

When the video is resized, the aspect ratio of the target can only approximate the aspect ratio of the source, the
difference is the aspect ratio error. `Smart Crop` (`S` in the crop window) crops a little more, 2 pixels on two
opposite sides per step, until the error is as small as possible. *Auto crop borders until proper aspect ratio is
found* in the [Project Options](User-Interface/Project-Options.md#crop) does it automatically.

## Crop values and color formats

With 4:2:0 color subsampling, which almost every video uses, the color has half the resolution of the brightness, so
only even crop values are possible. *Auto correct crop values* corrects the values accordingly and to the output mod,
the width and height the target must be divisible by.

## Dolby Vision

The metadata of Dolby Vision videos can contain the active area of every frame (level 5). StaxRipLinux
crops Dolby Vision sources according to it and writes the new values into the metadata, so the encode keeps
displaying correctly. *Auto Crop after opening* with `Dolby Vision only` does it automatically for Dolby Vision sources.
When the video is cut, the metadata is trimmed with dovi_tool.
