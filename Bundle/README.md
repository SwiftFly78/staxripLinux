# Bundled tools

The full AppImage of a release contains the tools StaxRipLinux uses, so they don't have to be installed. This
folder builds them from source in a Docker container with Ubuntu 22.04, the tools therefore run on distributions
with glibc 2.35 or newer, like the ones released since 2022.

## Building

Requires Docker.

```sh
Bundle/build.sh [<component>...]
```

builds the components of `components.list` and assembles the Apps folder of the full AppImage in
`Bundle/out/Apps` and the source code of everything it contains in `Bundle/out/sources`. Without arguments, the
components that aren't built yet are built, with arguments only the given ones. The full AppImage is built with:

```sh
APPS=Bundle/out/Apps Source/build-appimage.sh
```

A development build uses the bundled tools with a symbolic link from `Source/bin/Apps` to `Bundle/out/Apps`.

Downloads, build logs (`.cache/logs`) and built components are cached in `Bundle/.cache`. A component is built
again when its sources change, its build function or a helper of `lib.sh` it calls, the build image or a component
it depends on. With `KEEP_GOING=1` a failed component doesn't stop the build, the failed ones are listed at the end.

## Components

`components/<name>.sh` is the recipe of a component, the helpers it uses are in `lib.sh`. It defines:

- `version`, the version of the component.
- `packages`, the IDs of the StaxRipLinux packages (see `Source/General/Package.vb`) the component provides,
  one per line. Their versions are written to `Apps/versions.txt`.
- `depends`, the components it needs to build, they come before it in `components.list`.
- `sources`, one download per line: `<URL> <SHA-256> [<file name>]` or `git <URL> <commit> [<file name>]`.
- `binary_sources`, the file names of the sources that are binaries and not source code.
- `skip_submodules`, the names of submodules of its git sources the build doesn't need, they're left out of the
  archive.
- `build`, which builds the component and installs it into `$DESTDIR$PREFIX`, for example with
  `configure_make`, `cmake_build` or `meson_build`. `stage_license` adds license files.
- `bundle`, which copies its files from `$PREFIX` into the Apps folder with `apps_install` or `apps_plugin`.
  Tools go where the package looks for them in the Apps folder, see `Location` and `Locations` of the package.

Plugins are built with `cmake_plugin`, `meson_plugin`, `autotools_plugin` or plain `make`, then `plugin_install`
copies the built library under the file name StaxRipLinux looks for into the folder of its package, and
`plugin_bundle` copies it into the Apps tree. `port_plugin` builds AviSynth+ plugins that only have Visual Studio
projects, with stand-ins for the parts of the Windows API they use. The C libraries of Rust crates are built with
`cargo_c_install` and the toolchain of the `rust` component, cargo downloads the crates their `Cargo.lock` pins and
verifies them.

To update a component, change its version, URLs and checksums, and check the signature of a new source archive
if the project publishes one. CPU specific code must be detected at runtime, never use `-march=native`.

VapourSynth is R79, the last version that loads plugins of API 3, which many plugins still use, and the version
upstream StaxRip ships. The AviSynth and VapourSynth scripts are in `scripts`, see the README there.

## Libraries

`collect.py` copies the shared libraries the tools need into `Apps/lib` and sets a relative `RUNPATH`, so no
`LD_LIBRARY_PATH` is needed. Libraries every distribution has and libraries that have to match the system, like
glibc and the GPU drivers, come from the system: `excludelist` from
[pkg2appimage](https://github.com/AppImageCommunity/pkg2appimage) and `excludelist.local`. The libraries of
`fallbacklist` load the drivers of the system, like VA-API and the Vulkan loader, they come from the system when it
has them, otherwise from `Apps/lib/fallback`, so the tools that link them also start on systems without them. The
libraries of `dlopenlist`, like FFTW, are loaded by plugins with `dlopen`, every plugin finds `Apps/lib` by its
`RUNPATH`.

## Licenses and source code

`Apps/Licenses` contains the licenses of the components and of the libraries from Ubuntu packages, and a list of
them with their versions. The sources of the components are copied to `out/sources`, for libraries from Ubuntu
packages with a copyleft license their source packages are downloaded into `out/sources/ubuntu`. Every release
publishes `out/sources` together with this folder next to the AppImages.

## How StaxRipLinux uses the bundled tools

- The Apps folder is next to the executable, in the AppImage `usr/lib/staxrip/Apps`. Packages are found in the
  Apps folder before the system, so the bundled tools are used unless a custom path is defined in the apps dialog.
- The version of a bundled tool is the one of `Apps/versions.txt`, it isn't checked, and bundled tools can't be
  updated in the apps dialog.
- VapourSynth is the Python module of the bundled Python. VSScript finds the Python it loads in
  `~/.config/vapoursynth/vapoursynth.toml` by its own path, which changes with every start of the AppImage, so
  StaxRipLinux writes the entry at startup and removes the ones of former starts (`Source/General/Bundle.vb`).
