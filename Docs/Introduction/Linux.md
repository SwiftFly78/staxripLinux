# [Documentation](../README.md) / [Introduction](README.md) / Linux

> [!NOTE]
> StaxRipLinux is based on [StaxRip](https://github.com/staxrip/staxrip), its GTK 3 user interface uses
> [Eto.Forms](https://github.com/picoe/Eto). It was tested on Arch Linux with VapourSynth R80, Python 3.14, x264,
> x265, FFmpeg and MKVToolNix 102. StaxRipLinux doesn't depend on a specific distribution, it uses the tools and
> libraries found on the system, the full [AppImage](#appimage) brings the most important ones along.

## Tools

Besides the encoders, StaxRipLinux uses these tools:

| Task | Tools |
| --- | --- |
| AAC encoding | [fdkaac](https://github.com/nu774/fdkaac), AAC profiles in automatic mode use it if it's installed, otherwise the AAC encoder of FFmpeg |
| MPEG-2 sources | [D2V Witch](https://github.com/dubhater/D2VWitch) indexes MPEG-2 sources when a d2v source filter is selected, [D2VSource](https://github.com/Asd-g/MPEG2DecPlus) (AviSynth+) and [d2vsource](https://github.com/dwbuiten/d2vsource) (VapourSynth) open the d2v index |
| Script info | `Project > Advanced Script Info` shows the info of a script and benchmarks it with [avs2yuv](https://github.com/DJATOM/avs2yuv) (AviSynth+) or vspipe (VapourSynth) |
| Encoding AviSynth+ scripts | avs2yuv pipes AviSynth+ scripts to the encoders, without it FFmpeg is used, which requires an FFmpeg build with AviSynth support |
| Playback | [mpv](https://mpv.io) |
| Hardcoded subtitles | [SubText](https://github.com/vapoursynth/subtext) (VapourSynth) and [AssRender](https://github.com/pinterf/assrender) (AviSynth+, text subtitles only) |
| Source filters | The FFmpeg based source filters FFMS2, BestSource and L-SMASH Works |
| Audio decoding and cutting with AviSynth+ | FFAudioSource of FFMS2 opens the audio, FFmpeg decodes the script, so it must be built with AviSynth support |
| Cutting Dolby Vision videos | The editor of [dovi_tool](https://github.com/quietvoid/dovi_tool) trims the Dolby Vision metadata |

- VapourSynth scripts are played with mpv and passed to NVEncC, QSVEncC, VCEEncC and rav1e by piping them with vspipe,
  because many FFmpeg and encoder builds can't read VapourSynth scripts.
- The hardware decoding options of the encoders use VA-API (Intel, AMD) and CUDA (NVIDIA).
- The ffmpeg encoder has VA-API profiles (`ffmpeg | VAAPI | H.264`, `H.265` and `AV1`), which use the hardware
  encoders of AMD and Intel GPUs through Mesa or the Intel media driver without proprietary runtimes. The device
  is `/dev/dri/renderD128` unless another render node is defined in the encoder options.
- The SVT-AV1 forks Essential, HDR, PSYEX and Tritium all name their encoder `SvtAv1EncApp`. StaxRipLinux accepts the
  `SvtAv1EncApp` found in `PATH` for a fork only if its version output names that fork. Otherwise define the path of
  a fork build in the apps dialog (`Tools > Edit Path`) and confirm its version (`Version`, F12). Dolby Vision and
  HDR10+ require a build with `-DLIBDOVI_FOUND=ON -DLIBHDR10PLUS_RS_FOUND=ON` (libdovi and libhdr10plus-rs).
- StaxRipLinux runs eac3to with Wine through a command named `eac3to`, like the wrapper script of the AUR package
  `eac3to` (`wine /path/to/eac3to.exe "$@"`), and passes the paths as Wine paths (`Z:\...`), because eac3to reads
  arguments that start with `/` as options.

## Requirements

The full [AppImage](#appimage) contains the .NET runtime and the tools listed there, the slim AppImage only the
.NET runtime.

- 64-bit Linux (x86-64)
- [.NET 10 runtime](https://learn.microsoft.com/dotnet/core/install/linux), the [AppImages](#appimage) include it
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
  Tesseract (subtitle OCR), the subtitle plugins SubText and AssRender, the d2v source filters d2vsource and
  D2VSource, and libdovi (`libdovi.so.3`) for the Dolby Vision metadata of AV1 sources

Package names differ between distributions, some examples:

Arch Linux:

```sh
sudo pacman -S dotnet-runtime gtk3 libmediainfo mkvtoolnix-cli ffmpeg vapoursynth ffms2 \
    vapoursynth-plugin-bestsource x264 x265 svt-av1 aom rav1e opus-tools mpv
```

`fdkaac`, `avisynthplus`, `dovi-tool`, `hdr10plus-tool` (Dolby Vision and HDR10+ metadata) and `tesseract` are in the
official repositories as well. The AUR has packages for the other [tools](#tools) and plugins: `d2vwitch-git`,
`vapoursynth-plugin-d2vsource`, `vapoursynth-plugin-subtext-git` and, for AviSynth+, `avs2yuv-git`,
`avisynth-plugin-assrender-git` and `avisynth-plugin-mpeg2decplus-git` (D2VSource).

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
- The controls use the GTK theme. The themes of StaxRipLinux only define the colors of highlighting and other custom
  drawn content and are only used with a dark GTK theme.
- Settings are stored in `~/.config/StaxRip` (`$XDG_CONFIG_HOME/StaxRip`). A folder named `Settings` next to the
  `StaxRip` executable enables a portable installation, the environment variable `STAXRIP_SETTINGS` overrides both.
  The console help of tools is cached in `~/.cache/staxrip/Help` (`$XDG_CACHE_HOME`).

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
that runs without installation. Both AppImages contain the .NET runtime:

- `StaxRipLinux-<version>-x86_64.AppImage` also contains the tools and plugins StaxRipLinux works with:
  VapourSynth R79 with its own Python, AviSynth+ 3.7.6, FFmpeg with the common codecs and the hardware encoders of
  NVIDIA, Intel and AMD, the encoders x264, x265, SVT-AV1 and its forks, aomenc, rav1e, vvencFFapp, xvid_encraw,
  QSVEncC and VCEEncC, opusenc, fdkaac, the MKVToolNix command line tools, MP4Box, MediaInfo, mpv, dovi_tool,
  hdr10plus_tool, truehdd, 7-Zip, Tesseract and the AviSynth and VapourSynth plugins and scripts of StaxRip's
  filters that exist for Linux. It runs on distributions with glibc 2.35 or newer, like Ubuntu 22.04, Debian 12
  and Fedora 36. The hardware encoders and OpenCL plugins use the drivers of the system, mpv runs with X11 or
  XWayland. Not included are tools that may not be distributed or don't exist for Linux, like DEE, NeroAAC and
  eac3to, and NVEncC, which links NVIDIA's CUDA runtime that can't be combined with the GPL libraries of FFmpeg.
  The apps dialog shows how to install them.
- `StaxRipLinux-<version>-slim-x86_64.AppImage` uses the tools of the system, it needs the other
  [requirements](#requirements).

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
- The full AppImage uses its own tools, not the ones of the system. Another version of a tool can be used with a
  custom path, see [How StaxRipLinux finds tools and plugins](#how-staxriplinux-finds-tools-and-plugins). Python
  modules installed for the Python of the system aren't available to the VapourSynth of the AppImage.
- The licenses of the tools are in the folder `usr/lib/staxrip/Apps/Licenses` of the full AppImage, which
  `--appimage-mount` shows, the releases contain the source code of the tools.

## Updates

*Help > Check for Updates* compares the version of StaxRipLinux with the newest
[release](https://github.com/SwiftFly78/staxripLinux/releases). StaxRipLinux also checks about once a day, unless
*Check for updates approx. once per day* is disabled in the settings (*Startup*). When there is a newer version:

- *Download the AppImage* saves the AppImage of the release next to the running one, the full or the slim AppImage like
  the running one. It's executable and starts like the old one. When the old AppImage has its settings in a folder
  next to it (`<AppImage>.config`), the folder is copied for the new AppImage.
- *Open the release page* shows the changes and both AppImages.
- *Dismiss* hides this version until the next one is released.

StaxRipLinux keeps running until it's closed, the new AppImage is used from the next start on. Builds that don't run as
AppImage only get the link to the release page.

The tools of the full AppImage are updated with new releases of StaxRipLinux: *Auto Update* in the
[Apps Manager](../Usage/User-Interface/Apps-Manager.md) checks whether a newer release exists for the selected tool and
offers the full AppImage. Tools of the system are updated with the package manager, *Update Request* asks for a newer
version of a tool in the full AppImage.

## Building

Install the [.NET 10 SDK](https://learn.microsoft.com/dotnet/core/install/linux), clone this repository and run in
its `Source` folder:

```sh
dotnet publish StaxRip.vbproj -c Release -r linux-x64 --self-contained false -o ../publish
```

StaxRipLinux can be started from the output folder with `../publish/StaxRip`.

`./build-appimage.sh [<output folder>]` builds the slim AppImage. It needs
[appimagetool](https://github.com/AppImage/appimagetool) in `PATH`, or its path in the environment variable
`APPIMAGETOOL`. The full AppImage needs the tools it contains, they're built with Docker as described in
[Bundle](../../Bundle/README.md):

```sh
../Bundle/build.sh
APPS=../Bundle/out/Apps ./build-appimage.sh
```

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

StaxRipLinux has its own version, which the title bar shows. It follows [Semantic Versioning](https://semver.org),
began with v1.0.0 at the commit that ported StaxRip to Linux, and every commit gets an annotated tag with its version.
The tags of StaxRip itself, like v2.52.6, aren't versions of StaxRipLinux. The assembly version is still the one of
the StaxRip version StaxRipLinux is based on, the settings use it. The update check compares the version of
StaxRipLinux with the releases, see [Updates](#updates).

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

It tags the commits of StaxRipLinux up to HEAD that have no version yet, from the oldest one, and prints the command to
push the tags. `Source/version.sh -n` only shows the versions, `Source/version.sh current` shows the version of HEAD.
Fetch the tags of other contributors first (`git fetch --tags`), so that no version is used twice. The script needs
[node-semver](https://github.com/npm/node-semver), the package `semver` on Arch Linux and `node-semver` on Debian and
Ubuntu, otherwise it runs node-semver with `npx` (Node.js).

### Releases

The workflow [CI](../../.github/workflows/ci.yml) builds the bundled tools and the full AppImage of every pull
request and push of `master` and runs the [tests](../../Tests/README.md), a ruleset of `master` requires them to pass.
After the tests of a push of `master`, CI tags the new commits without version, like the merged pull requests, with
`Source/version.sh` and starts the workflow [Release](../../.github/workflows/release.yml). It builds the AppImages of
the newest version and publishes them with the source code of the bundled tools as GitHub release, the release notes
list the versions since the previous release with the subjects of their commits. Commits pushed with their tags keep
them:

```sh
git push origin HEAD:master v1.15.0 v1.15.1
```

Merge pull requests with *Squash and merge*, so that every commit of `master` gets one version. Versions that have a
release already are skipped. *Run workflow* on the page of the workflow Release in the *Actions* tab of the repository
releases the newest version if it has no release yet, for example after a failed run. CI saves the build cache of the
bundled tools of `master` in GitHub Actions, pull requests and releases only build the components that changed since.
[Renovate](../../Bundle/README.md#updates) recommends the updates of the bundled tools and the other dependencies in
one pull request a week.
