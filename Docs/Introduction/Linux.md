# [Documentation](../README.md) / [Introduction](README.md) / Linux

> [!NOTE]
> StaxRipLinux is a port of StaxRip with a GTK 3 user interface based on [Eto.Forms](https://github.com/picoe/Eto).
> It was tested on Arch Linux with VapourSynth R80, Python 3.14, x264, x265, FFmpeg and MKVToolNix 102.
> StaxRipLinux doesn't depend on a specific distribution, it uses the tools and libraries found on the system.

## Differences to the Windows version

- StaxRipLinux doesn't include any tools. It uses the tools, libraries and plugins installed on the system,
  usually with the package manager of the distribution.
- Tools that only exist for Windows are hidden in the apps dialog, Linux tools take over their tasks:

  | Windows | Linux |
  | --- | --- |
  | qaac | [fdkaac](https://github.com/nu774/fdkaac), AAC profiles in automatic mode use it if it's installed, otherwise the AAC encoder of FFmpeg |
  | DGIndex | [D2V Witch](https://github.com/dubhater/D2VWitch) indexes MPEG-2 sources when a d2v source filter is selected |
  | DGDecode | [D2VSource](https://github.com/Asd-g/MPEG2DecPlus) (AviSynth+) and [d2vsource](https://github.com/dwbuiten/d2vsource) (VapourSynth) open the d2v index |
  | AVSMeter | `Project > Advanced Script Info` shows the info of a script and benchmarks it with [avs2yuv](https://github.com/DJATOM/avs2yuv) (AviSynth+) or vspipe (VapourSynth) |
  | avs2pipemod | [avs2yuv](https://github.com/DJATOM/avs2yuv) pipes AviSynth+ scripts to the encoders, without it FFmpeg is used, which requires an FFmpeg build with AviSynth support |
  | MPC, mpv.net | [mpv](https://mpv.io) |
  | VSFilterMod, xy-VSFilter | Hardcoded subtitles use [SubText](https://github.com/vapoursynth/subtext) (VapourSynth) and [AssRender](https://github.com/pinterf/assrender) (AviSynth+, text subtitles only) |
  | DirectShowSource, DSS2, AviSource (DirectShow, VFW) | The FFmpeg based source filters FFMS2, BestSource and L-SMASH Works |
  | NicAudio and DirectShow audio decoding and cutting | FFAudioSource of FFMS2 with AviSynth+, FFmpeg decodes the script, so it must be built with AviSynth support |
  | AutoCrop | Auto crop runs inside StaxRipLinux |
  | DoViSplit | The editor of [dovi_tool](https://github.com/quietvoid/dovi_tool) trims the Dolby Vision metadata when the video is cut |

- VapourSynth scripts are played with mpv and passed to NVEncC, QSVEncC, VCEEncC and rav1e by piping
  them with vspipe, because many FFmpeg and encoder builds can't read VapourSynth scripts.
- The hardware decoding options of the encoders use VAAPI (Intel, AMD) and CUDA (NVIDIA) instead of DXVA2.
- The ffmpeg encoder has VA-API profiles (`ffmpeg | VAAPI | H.264`, `H.265` and `AV1`), which use the hardware
  encoders of AMD and Intel GPUs through Mesa or the Intel media driver without proprietary runtimes. The device
  is `/dev/dri/renderD128` unless another render node is defined in the encoder options.
- The SVT-AV1 forks Essential, HDR, PSYEX and Tritium all name their encoder `SvtAv1EncApp`. StaxRipLinux accepts the
  `SvtAv1EncApp` found in `PATH` for a fork only if its version output names that fork. Otherwise define the path of
  a fork build in the apps dialog (`Tools > Edit Path`) and confirm its version (`Version`, F12). Dolby Vision and
  HDR10+ require a build with `-DLIBDOVI_FOUND=ON -DLIBHDR10PLUS_RS_FOUND=ON` (libdovi and libhdr10plus-rs).
- eac3to only exists for Windows. StaxRipLinux runs it with Wine when a command named `eac3to` starts it, like the
  wrapper script of the AUR package `eac3to` (`wine /path/to/eac3to.exe "$@"`). StaxRipLinux passes the paths as Wine
  paths (`Z:\...`), because eac3to reads arguments that start with `/` as options.
- Thumbnail sheets can be saved as JPEG, PNG, BMP and TIFF, but not as GIF.
- The controls use the GTK theme. The themes of StaxRipLinux only define the colors of highlighting and other custom
  drawn content and are only used with a dark GTK theme.
- Settings are stored in `~/.config/StaxRip` (`$XDG_CONFIG_HOME/StaxRip`). A folder named `Settings` next to the
  `StaxRip` executable enables a portable installation, the environment variable `STAXRIP_SETTINGS` overrides both.
  The console help of tools is cached in `~/.cache/staxrip/Help` (`$XDG_CACHE_HOME`).

## Requirements

- 64-bit Linux (x86-64)
- [.NET 10 runtime](https://learn.microsoft.com/dotnet/core/install/linux), the [AppImage](#appimage) includes it
- GTK 3
- MediaInfo library (`libmediainfo`)
- MKVToolNix command line tools
- FFmpeg
- VapourSynth R55 or newer with source filter plugins like ffms2, BestSource or L-SMASH Works,
  or AviSynth+ (`libavisynth.so`) with plugins
- The encoders you want to use, for example x264, x265, SvtAv1EncApp, aomenc, rav1e, vvencFFapp,
  NVEncC (`nvencc`), QSVEncC (`qsvencc`) or VCEEncC (`vceencc`). vvenc installs vvencFFapp only when it's built
  with `-DVVENC_INSTALL_FULLFEATURE_APP=ON`. VCEEncC and the AMF encoders of FFmpeg require AMD's AMF runtime
  (`libamfrt64.so.1`, `amf-amdgpu-pro` in the AUR).
- Optional: mpv, opusenc, fdkaac, dovi_tool, hdr10plus_tool, D2V Witch, avs2yuv, eac3to (with Wine),
  Tesseract (subtitle OCR), the subtitle plugins SubText and AssRender and the d2v source filters d2vsource and
  D2VSource

Package names differ between distributions, some examples:

Arch Linux:

```sh
sudo pacman -S dotnet-runtime gtk3 libmediainfo mkvtoolnix-cli ffmpeg vapoursynth ffms2 \
    vapoursynth-plugin-bestsource x264 x265 svt-av1 aom rav1e opus-tools mpv
```

`fdkaac`, `avisynthplus`, `dovi-tool`, `hdr10plus-tool` (Dolby Vision and HDR10+ metadata) and `tesseract` are in the
official repositories as well. The AUR has packages for the other tools and plugins that replace the Windows tools:
`d2vwitch-git`, `vapoursynth-plugin-d2vsource`, `vapoursynth-plugin-subtext-git` and, for AviSynth+,
`avs2yuv-git`, `avisynth-plugin-assrender-git` and `avisynth-plugin-mpeg2decplus-git` (D2VSource).

Fedora (x264 and x265 are available from [RPM Fusion](https://rpmfusion.org), which also has a complete FFmpeg):

```sh
sudo dnf install dotnet-runtime-10.0 gtk3 libmediainfo mkvtoolnix ffmpeg-free python3-vapoursynth \
    vapoursynth-tools ffms2 mpv
```

Debian 13 (Debian has no VapourSynth package, it can be installed with pipx as described below,
the .NET runtime is available from [Microsoft](https://learn.microsoft.com/dotnet/core/install/linux-debian)):

```sh
sudo apt install libgtk-3-0t64 libmediainfo0v5 mkvtoolnix ffmpeg libffms2-5 x264 x265 mpv pipx
```

VapourSynth from the Python Package Index works on every distribution. It includes vspipe, StaxRipLinux finds the
VapourSynth libraries in the folder of the Python module:

```sh
pipx install vapoursynth
```

Tesseract is packaged as `tesseract` (Arch Linux, Fedora) or `tesseract-ocr` (Debian, Ubuntu), its languages as
`tesseract-data-eng`, `tesseract-langpack-eng` or `tesseract-ocr-eng`. StaxRipLinux can also download languages,
see [Subtitle OCR](#subtitle-ocr).

### How StaxRipLinux finds tools and plugins

- Executables are searched in `PATH`, also by common alternative names, for example `7zz` and `7za` for 7-Zip.
- Libraries are searched in `LD_LIBRARY_PATH`, the standard and multiarch directories, the directories of the
  dynamic linker configuration (`/etc/ld.so.conf`) and the profiles of Nix and Guix. Versioned files like
  `libffms2.so.5` are found when the unversioned name of the development package is missing, also AviSynth+
  plugins built with CMake, which contain the version in the name like `libd2vsource.1.3.0.so`.
- VapourSynth plugins are additionally searched in the folders VapourSynth loads plugins from automatically
  (`site-packages/vapoursynth/plugins` and `VAPOURSYNTH_EXTRA_PLUGIN_PATH`), `~/.local/lib/vapoursynth`,
  `~/.local/share/vapoursynth/plugins`, the `vapoursynth` subdirectory of every library directory and
  `Plugins/VapourSynth` in the settings folder. Plugins that VapourSynth loads automatically are detected and
  not loaded a second time.
- AviSynth+ plugins are searched in `~/.avisynth`, `~/.local/lib/avisynth`, the `avisynth` subdirectories and
  `Plugins/AviSynth` in the settings folder.
- A custom path can be defined for every tool in the apps dialog (`Apps > Manage...`, then `Tools > Edit Path...`).

> [!TIP]
> Recent VapourSynth versions need a configuration that tells VapourSynth which Python installation to use.
> It's usually created automatically. If StaxRipLinux reports that VapourSynth can't find Python, run
> `vapoursynth config`.

### Desktop integration

- Files, folders and URLs are opened with `xdg-open`, or `gio`, `kde-open` or `exo-open` if it's missing.
  `Explore` selects the file in the file manager using the `org.freedesktop.FileManager1` D-Bus interface and
  opens the folder if the file manager doesn't support it.
- Console apps and their help are shown in a terminal emulator. It can be defined in the settings
  (`Tools > Settings > System > Terminal`), otherwise the `TERMINAL` environment variable is used, then
  `xdg-terminal-exec`, `x-terminal-emulator` and common terminals like Konsole, GNOME Terminal, Ptyxis,
  Xfce Terminal, Alacritty, kitty, foot, WezTerm and xterm.
- While encoding, standby is prevented with `systemd-inhibit` or `elogind-inhibit`. Shutdown and standby after
  the jobs use `systemctl` or `loginctl`.

## Subtitle OCR

StaxRipLinux converts bitmap subtitles, PGS (`.sup`) of Blu-ray discs and VobSub (`.idx`/`.sub`) of DVDs, to SRT text
subtitles with the OCR engine [Tesseract](https://github.com/tesseract-ocr/tesseract). Text subtitles are small, can be
edited and are supported by more containers and players, MP4 for example doesn't support PGS.

- *OCR to SRT...* on the *Subtitles* tab of the container options (*Container Options* in the main window) converts the
  selected bitmap subtitle. The SRT file replaces the subtitle in the list, its language, name and flags are kept.
- *Convert bitmap subtitles (PGS, VobSub) to SRT using OCR* in the subtitle options (`Project > Options > Subtitles`)
  converts the demuxed bitmap subtitles when a source is opened.
- The OCR uses the language of the subtitle. Subtitles with an undetermined language use English, or another installed
  language if English isn't installed.
- Tesseract finds the languages installed with the package manager. Missing languages can be downloaded from the
  [tessdata_fast](https://github.com/tesseract-ocr/tessdata_fast) repository of Tesseract, StaxRipLinux asks before it
  downloads a language and saves it in the `tessdata` folder of the settings. *OCR Languages...* in the subtitle options
  downloads more languages. Job processing doesn't download languages, subtitles whose language isn't installed stay
  bitmap subtitles.
- The SRT files are named like the bitmap subtitles and saved in the temp folder of the project.
- OCR makes mistakes, check the text. Italic text loses its formatting, and the text must be brighter than its outline,
  like in most subtitles.

## AppImage

The [releases](https://github.com/SwiftFly78/staxripLinux/releases) contain StaxRipLinux as AppImage, a single file
that runs without installation. It includes the .NET runtime, the other [requirements](#requirements) are still needed.
Make the file executable and start it:

```sh
chmod +x StaxRipLinux-*-x86_64.AppImage
./StaxRipLinux-*-x86_64.AppImage
```

- AppImages need FUSE, which the package `fuse3` provides. Without FUSE, start the AppImage with
  `--appimage-extract-and-run`.
- The settings are stored in `~/.config/StaxRip`. A folder next to the AppImage with its name and `.config` appended,
  like `StaxRipLinux-1.15.0-x86_64.AppImage.config`, keeps them next to the AppImage instead.
- Tools like Gear Lever or AppImageLauncher add AppImages to the application menu.

## Building

Install the [.NET 10 SDK](https://learn.microsoft.com/dotnet/core/install/linux), clone this repository and run in
its `Source` folder:

```sh
dotnet publish StaxRip.vbproj -c Release -r linux-x64 --self-contained false -o ../publish
```

StaxRipLinux can be started from the output folder with `../publish/StaxRip`.

`./build-appimage.sh [<output folder>]` builds an AppImage like the ones of the releases. It needs
[appimagetool](https://github.com/AppImage/appimagetool) in `PATH`, or its path in the environment variable
`APPIMAGETOOL`.

## Installation

The build output contains an install script that copies StaxRipLinux to `~/.local/lib/staxrip`, creates the
`~/.local/bin/staxrip` command and adds StaxRipLinux with its icon to the application menu:

```sh
../publish/Linux/install.sh
```

Use `PREFIX=/usr/local` (as root) for a system wide installation and `install.sh --uninstall` to remove StaxRipLinux.
The settings in `~/.config/StaxRip` are kept when StaxRipLinux is removed.

On Wayland, desktops show the icon of the installed `staxrip.desktop` file in the title bar and task bar,
when StaxRipLinux is started without installation, a generic icon is shown instead.

## Troubleshooting

- Start StaxRipLinux from a terminal with the `staxrip` command, errors are also written to the terminal.
- If StaxRipLinux can't find a tool, the apps dialog shows its status and how to install it or define a custom path.
- If a VapourSynth script fails with a missing namespace like `sub` or `ffms2`, the plugin isn't installed or not
  in a folder StaxRipLinux searches, see above.
- StaxRipLinux uses classic scrollbars. Set the environment variable `GTK_OVERLAY_SCROLLING=1` to use overlay
  scrollbars.

## Versioning

The Linux port has its own version, which the title bar shows. It follows [Semantic Versioning](https://semver.org),
began with v1.0.0 at the commit that ported StaxRip to Linux, and every commit gets an annotated tag with its version.
The tags of StaxRip itself, like v2.52.6, aren't versions of the port. The assembly version is still the one of the
StaxRip version the port is based on, settings and the update check use it.

The build takes the version from the tags with `git describe`, so it needs a clone that contains them. A commit without
a tag yet gets the version of the last tag with the number of commits since then and the commit, like
`v1.10.159-2-g1a2b3c4`, and uncommitted changes add `-dirty`. Builds without git show no version. The build also takes
the versions with the subjects of their commits from the tags, *Help > What's new...* lists them.

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org), the version of a commit is derived
from its message:

| Commit message | Version |
|---|---|
| `!` after the type or scope like `feat!: ...`, or a `BREAKING CHANGE: ...` footer | Major |
| `feat: ...` | Minor |
| `fix:`, `build:`, `chore:`, `ci:`, `docs:`, `perf:`, `refactor:`, `revert:`, `style:`, `test:` | Patch |

Unlike in the specification, every type changes at least the patch version, so that each commit has its own version.
Merge commits and reverts with the messages git writes change the patch version as well, a merge that brings new
features, like a newer StaxRip version, needs a message like `feat: merge StaxRip v2.53.0`. The commits that are older
than this convention were tagged retroactively up to v1.10.23.

After committing, tag the new commits:

```sh
Source/version.sh
```

It tags the commits of the port up to HEAD that have no version yet, from the oldest one, and prints the command to
push the tags. `Source/version.sh -n` only shows the versions, `Source/version.sh current` shows the version of HEAD.
Fetch the tags of other contributors first (`git fetch --tags`), so that no version is used twice. The script needs
[node-semver](https://github.com/npm/node-semver), the package `semver` on Arch Linux and `node-semver` on Debian and
Ubuntu, otherwise it runs node-semver with `npx` (Node.js).

### Releases

When a push brings new version tags to `master`, the workflow [Release](../../.github/workflows/release.yml) builds an
AppImage of the newest version and publishes it as GitHub release. The release notes list the versions since the
previous release with the subjects of their commits. The tags can be pushed before, with or after the branch, but
GitHub starts no workflow for the tags of a push with more than three tags, so push them together with the branch:

```sh
git push origin HEAD:master v1.15.0 v1.15.1
```

Versions that have a release already are skipped. *Run workflow* on the page of the workflow in the *Actions* tab of
the repository releases the newest version if it has no release yet, for example after a failed run.
