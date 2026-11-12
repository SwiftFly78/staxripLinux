# [Documentation](../../README.md) / [Tools](../README.md) / [Supported Encoders](README.md) / Audio Encoders

## Profiles

| Profile | Description |
|---|---|
| AAC ~318 Kbps | AAC |
| AC3 640 Kbps | Dolby Digital, 5.1 |
| EAC3 640 Kbps | Dolby Digital Plus, 5.1 |
| FLAC ~2534 Kbps | Lossless |
| MP3 ~165 Kbps | MP3 |
| Opus ~250 Kbps | Opus |
| Vorbis ~252 Kbps | Vorbis |
| Command Line | Any encoder with a command line, by default AC3 with ffmpeg |
| Copy/Mux | Copies the track without encoding |
| No Audio | Leaves the track out |

Profiles with a quality based mode show the estimated bitrate in their name. The codec, encoder, channels, bitrate and
all other options of
a profile are set in the [Audio Settings](../../Usage/User-Interface/Audio-Settings.md).

## Encoders

| Encoder | Codecs | Full AppImage |
|---|---|---|
| [ffmpeg](https://ffmpeg.org) | AAC, AC3, EAC3, FLAC, MP3, Opus, Vorbis, W64, WAV | ✓ |
| [fdkaac](https://github.com/nu774/fdkaac) | AAC (LC, HE-AAC, HE-AACv2, LD, ELD) | ✓ |
| [opusenc](https://opus-codec.org) | Opus | ✓ |
| [eac3to](https://en.wikibooks.org/wiki/Eac3to) | AAC, AC3, DTS, FLAC, W64, WAV | runs with Wine |
| [DeeZy](https://github.com/jlw4049/DeeZy) | AC3, EAC3, AC4 with the Dolby Encoding Engine | |

`Automatic` chooses the encoder by the codec, AAC uses fdkaac if it's installed, otherwise FFmpeg. eac3to also
decodes formats like TrueHD and DTS-HD, `Decoder` in the [Audio Settings](../../Usage/User-Interface/Audio-Settings.md)
chooses the tool that decodes the source.
