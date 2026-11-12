# [Documentation](../../README.md) / [Guides](../README.md) / [Encoder related](README.md) / Audio

## Copy or encode

- Lossy tracks (AC3, EAC3, DTS, AAC) are copied with `Copy/Mux`, encoding them again only loses quality.
- Lossless tracks (TrueHD, DTS-HD MA, PCM, FLAC) can be copied for the best quality, or encoded to save space.
- Tracks with Dolby Atmos or DTS:X keep their objects only when they're copied.
- Commentary tracks and secondary languages can be encoded with a low bitrate.

The audio profile of a track is chosen on the [Audio](../../Usage/User-Interface/Main.md#audio) card, `Edit...` opens
its [Audio Settings](../../Usage/User-Interface/Audio-Settings.md).

## Codecs and encoders

| Codec | Recommended encoder | Use |
|---|---|---|
| Opus | opusenc or ffmpeg | The most efficient codec, for MKV and WebM |
| AAC | fdkaac, else ffmpeg | Plays on every device, the codec for MP4 |
| AC3 | ffmpeg | Surround for AV receivers and older devices |
| EAC3 | ffmpeg | Surround with less bitrate than AC3, Dolby Digital Plus |
| FLAC | ffmpeg | Lossless with about half the size of PCM |
| DTS | eac3to | For devices that need DTS |
| AC4, EAC3 with DEE | DeeZy | Dolby's own encoder, which has to be installed separately |

- **fdkaac** produces the best AAC, especially at low bitrates and with HE-AAC. It's part of the full AppImage.
- **ffmpeg** encodes almost every codec, its native AAC encoder is good at 160 Kbps and more for stereo.
- **opusenc** is the reference encoder of Opus.
- **eac3to** runs with Wine, see [Tools](../../Introduction/Linux.md#tools).
- **DeeZy** uses the Dolby Encoding Engine (DEE), which isn't freely available.

## Recommended settings

- **Stereo**: Opus 128 Kbps or AAC 160 to 192 Kbps.
- **5.1**: Opus 256 Kbps, EAC3 384 to 640 Kbps or AC3 448 to 640 Kbps.
- **Archiving**: FLAC, or copy the original track.

AAC and Opus have quality based modes (VBR), which are set with *Quality* instead of *Bitrate*. The default AAC
profile encodes with quality based VBR.

## Several tracks at once

The audio tracks are encoded in parallel with the video, as far as *Maximum number of parallel processes* in the
[Settings](../../Usage/User-Interface/Settings.md#general) allows. The [next
step](../../Usage/User-Interface/Main.md#next-step)
bar warns when the same track is used twice.
