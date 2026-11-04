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

- `version`, the version of the component, with a `# renovate:` comment that tells Renovate where to look for
  new versions, see [Updates](#updates). Sources with versions of their own get variables like `zimg_version`.
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

Renovate recommends the updates of the components, see [Updates](#updates). CPU specific code must be detected at
runtime, never use `-march=native`.

VapourSynth is R79, the last version that loads plugins of API 3, which many plugins still use, and the version
upstream StaxRip ships. The AviSynth and VapourSynth scripts are in `scripts`, see the README there.

## Updates

[Renovate](https://docs.renovatebot.com) recommends every update as a pull request: of the components, the NuGet
packages, the actions of the workflows, the base images, the build tools of `requirements-build.txt` and the
appimagetool and AppImage runtime of `Source/appimage-tools.sh`. It runs in `.github/workflows/renovate.yml` with the
configuration `.github/renovate.json5` and creates the branches of new updates on Fridays at noon (Europe/Berlin):

- A release is proposed two weeks after its publication, when broken and malicious releases are usually known.
- Every component gets a pull request of its own, at most ten are open at a time. The base image and the build
  tools are grouped and proposed on the first Friday of a month, a new build image rebuilds every component.
- Commits of a branch, the source of components without releases, are proposed on the first Friday of a month.
- Known breaks aren't proposed by themselves: major versions of FFmpeg (L-SMASH-Works), VapourSynth R80 and newer
  (plugins of API 3), libplacebo newer than 7.360 (QSVEncC, VCEEncC), BestSource R22 and newer (VapourSynth R79) and
  major versions of nv-codec-headers (minimum NVIDIA driver). The Dependency Dashboard issue lists them with every
  other pending update, a tick there opens the pull request at once, outside of the schedule too.

The `# renovate:` comment of a version names the datasource and package of its upstream, for example:

```sh
version=8.1.3 # renovate: datasource=github-tags depName=FFmpeg/FFmpeg extractVersion=^n(?<version>.+)$
version=1.0-d00a374 # renovate: datasource=git-refs depName=https://github.com/o/r.git branch=master digest=<commit>
```

Renovate changes the version (or the digest), `renovate.py post-upgrade` completes the update in the same commit: the
commit of the tag of a git source, the URLs and checksums of downloads and the names in `build` and `bundle`.
`source=<n>[,<n>]` assigns the version to lines of `sources`, `none=<reason>` marks a component without updates.
`versioning=loose` compares versions with four numbers or a suffix, versions like `r12` or `R21` need
`versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$`, Renovate ignores components whose version it can't read.
Loose versioning ranks `3.0.2` above `3.0.2-B`, suffixes of later builds of a version like `3.0.2-B` or `1.1.1e` need
`versioning=deb`. `extractVersion` keeps only the tags of the project's own releases, forks like SVT-AV1-Essential
also have the tags of their upstream, and has the form `^<prefix>(?<version>...)$` without groups in the version,
post-upgrade derives the tag from it.
A `revision=` of a recipe is the commit count of its git source, which archives lack, post-upgrade counts it for a new
commit (AviSynth+ shows it in its version string). `renovate.py check --online` verifies the comments of all recipes
and the revisions, run it after adding a component.

The CI workflow tests every pull request: it builds the changed components with everything that depends on them,
builds the AppImages and runs the test suites of `Tests`, see the README there. An update is merged when its tests
pass, with "Squash and merge", every commit of master gets a version. CI then tags the commit with
`Source/version.sh` and releases it.

Renovate runs as a GitHub App of the repository, the pull requests of the token of a workflow wouldn't start CI. To
set it up once:

1. Create a GitHub App (Settings, Developer settings, GitHub Apps, New GitHub App) without webhook, with the
   repository permissions Checks, Commit statuses, Contents, Issues, Pull requests and Workflows (read and write) and
   Administration and Dependabot alerts (read), and install it on the repository. Renovate aborts its run when it
   can't set a commit status, before it creates the Dependency Dashboard. Changed permissions of the App apply after
   their approval in the installation (Settings, Applications, Configure).
2. Store its Client ID as the variable `RENOVATE_CLIENT_ID` and a private key of it as the secret
   `RENOVATE_APP_PRIVATE_KEY` of the repository (Settings, Secrets and variables, Actions).
3. Protect master with a ruleset that requires the status check `Tests passed` of CI, with the role Repository admin
   in its bypass list, so pushes of the maintainers to master still go through, CI tests them after the push.
4. Allow only "Squash and merge" for pull requests (Settings, General, Pull Requests).

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
