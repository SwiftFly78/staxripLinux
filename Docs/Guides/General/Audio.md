# [Documentation](../../README.md) / [Guides](../README.md) / [General](README.md) / Audio

## Lossless and lossy

- **Lossless** codecs keep every bit of the original: PCM (WAV), FLAC, TrueHD, DTS-HD Master Audio. Blu-ray discs
  often contain lossless tracks, which need 2 to 5 Mbps or more.
- **Lossy** codecs remove what is hardly audible: AAC, Opus, AC3 (Dolby Digital), EAC3 (Dolby Digital Plus), DTS,
  MP3, Vorbis. They need a fraction of the bitrate.

Converting one lossy codec to another loses quality each time, so lossy tracks are best copied (`Copy/Mux`). Lossless
tracks can be copied for the best quality or encoded to save space.

## Channels

Stereo has 2 channels, 5.1 surround 6 and 7.1 surround 8. *Channels* in the
[Audio Settings](../../Usage/User-Interface/Audio-Settings.md) can downmix to fewer channels, for example 5.1 to stereo
for headphones and TV speakers. *Center/Speech optimized Stereo* keeps dialogues clear, which come from the center
channel.

Object based formats, Dolby Atmos in TrueHD or EAC3 and DTS:X, can only be kept by copying the track. Encoding them
keeps the channel based part.

## Bitrates

Typical bitrates for transparent quality, which most people can't tell from the original:

| Codec | Stereo | 5.1 |
|---|---|---|
| Opus | 128 Kbps | 256 to 320 Kbps |
| AAC | 160 to 192 Kbps | 384 to 448 Kbps |
| AC3 | 192 to 256 Kbps | 448 to 640 Kbps |
| EAC3 | 192 Kbps | 384 to 640 Kbps |

Opus is the most efficient, AAC plays on every device, AC3 and EAC3 can be passed to AV receivers by most players.

## Sampling rate

Most videos have 48 kHz, CDs 44.1 kHz. *Original* keeps the sampling rate, which is right in almost all cases. Opus
always encodes with 48 kHz.

## Delay

Audio tracks can start earlier or later than the video, demuxed tracks contain the delay in their file name, like
`DELAY 40ms`. StaxRipLinux reads it and the muxer applies it, so the audio stays in sync. When the video is cut, the
audio is cut the same way.

## Normalization and gain

*Gain* changes the volume by a fixed value in dB. *Normalize* adjusts the volume automatically: to the peak level, to a
loudness standard (EBU R128) or dynamically. Normalizing a movie track changes its dynamics, so it's mostly useful for
quiet recordings.
