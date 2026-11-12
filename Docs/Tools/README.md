# [Documentation](../README.md) / Tools

StaxRipLinux is a graphical front end: the work is done by console apps, libraries and plugins, which StaxRipLinux
configures and runs. Every step is logged with its command line, so every result can be reproduced.

| Task | Tools |
|---|---|
| Media info | MediaInfo |
| Demuxing | mkvextract, MP4Box, ffmpeg, eac3to, D2V Witch |
| Indexing and decoding | FFMS2, L-SMASH Works, BestSource, D2V source filters |
| Video processing | VapourSynth, AviSynth+ and their plugins |
| Video encoding | x264, x265, SvtAv1EncApp, aomenc, rav1e, vvencFFapp, NVEncC, QSVEncC, VCEEncC, ffmpeg |
| Audio encoding | ffmpeg, fdkaac, opusenc, eac3to, DeeZy |
| HDR metadata | dovi_tool, hdr10plus_tool |
| Subtitles | Tesseract (OCR), BDSup2Sub++, Subtitle Edit |
| Muxing | mkvmerge, MP4Box, ffmpeg |
| Playback | mpv |

- [Supported Tools](Supported-Tools.md): all tools, plugins and scripts
- [Supported Encoders](Supported-Encoders/README.md): the video and audio encoders with their profiles
    - [Video Encoders](Supported-Encoders/Video.md)
    - [Audio Encoders](Supported-Encoders/Audio.md)

The full AppImage contains most of them, the [Apps Manager](../Usage/User-Interface/Apps-Manager.md) shows the status
of every tool and how to install missing ones. See [Linux](../Introduction/Linux.md#tools) for the tools StaxRipLinux
uses for particular tasks and how it finds them.
