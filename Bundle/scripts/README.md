# Scripts

The AviSynth and VapourSynth scripts of the packages StaxRipLinux knows, like QTGMC.avsi or havsfunc.py. They're
copied unchanged from the Apps folder of the
[StaxRip v2.52.6](https://github.com/staxrip/staxrip/releases/tag/v2.52.6) release, the versions StaxRip's filter
profiles are made for. The release is about 1 GB of Windows tools, so the scripts are kept here instead of being
extracted from it on every build. Their licenses are in the files or next to them.

`Plugins` has the layout of the Apps folder, a script is where StaxRipLinux looks for it: in the location of its
package (see `Location` in `Source/General/Package.vb`), otherwise in `Plugins/AVS/<package name>` or
`Plugins/VS/<package name>`. Two differ from the release: `InterFrame.avsi` is in `Plugins/AVS/InterFrame2` and
`Maa2mod.avsi` is named `maa2mod.avsi`, the names the packages use.
