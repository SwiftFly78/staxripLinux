# Tests

The test suites of the full AppImage, CI runs them for every pull request and push of master (see
`.github/workflows/ci.yml`), so an update of a bundled tool can't break a tool, plugin or job that depends on it. They
run in a desktop-like Ubuntu 22.04 without any of the bundled tools (`Dockerfile`), the oldest system the AppImage
supports, so missing libraries show up. The media of the tests are generated with the bundled tools
(`container/media.sh`).

```sh
Tests/run.sh <AppImage> <suite>...
```

| Suite | Tests |
|---|---|
| `static` | the libraries and symbols of every bundled file, that every AviSynth+ and VapourSynth plugin loads and has the functions StaxRipLinux calls, that every executable starts |
| `tools` | a small real job of every bundled tool: encoders, the encoders of FFmpeg that the profiles use, audio encoders, muxers, demuxers, the HDR tools, the frame servers, the plugins no filter profile uses |
| `options` | that the encoders know every switch the options of their StaxRipLinux profiles can pass: a switch their help doesn't name must not be rejected as unknown, so removed or renamed switches show up |
| `frames` | every default filter profile of both frame servers, with every option of its menus, renders frames of the script StaxRipLinux generates for it, Vulkan and OpenCL with the software drivers lavapipe and PoCL |
| `pipelines` | StaxRipLinux jobs: every encoder profile with both frame servers, every muxer, every source filter with every source it can open, every audio encoder with the decoders, downmixing and normalization, and features like multi-pass encoding, chunks, cutting, cropping, deinterlacing, hard subtitles, HDR10, Dolby Vision and VFR |

`all` runs them all. `pipelines:<n>/<count>` runs every count-th job, CI splits them into six parallel jobs.
`pipelines.py` generates the jobs from the profiles StaxRipLinux has (`container/inventory.ps1`), so new profiles are
tested automatically, and checks the targets with ffprobe: codec, frames, size, audio tracks, HDR10 and Dolby Vision.

The results are in `Tests/.work/results/<suite>.tsv`, `report.py` summarizes them. `known-issues.txt` lists the
failures the tests expect with their reasons, and mostly with the error they expect, so another error of the same test
still fails. A known issue that passes again is reported so the list stays current. Every other failure fails CI. To debug a test, `Tests/.work` keeps the scripts of the frame tests with their errors,
the results, logs and screenshots of the jobs, and

```sh
docker run --rm -v <AppImage>:/app.AppImage:ro -v $PWD/Tests/container:/t:ro -v $PWD/Tests/.work:/work \
    <image of run.sh> bash /t/staxrip.sh script /work/<script>.ps1
```

runs a PowerShell script in a StaxRipLinux session with the media in `/tmp/media`.
