"""Generates the StaxRip jobs of the pipeline tests and validates their results.

    pipelines.py generate <inventory.json> <specs folder> [--shard <i>/<n>] [--match <regex>]
    pipelines.py validate <specs folder> <out folder> <results file>

The jobs cover every encoder profile that runs without a GPU with both frame servers, every muxer, every source filter
with every source it can open, every audio encoder with the decoders, downmixing and normalization, and features like
multi-pass encoding, chunks, cutting, cropping, deinterlacing, hard subtitles, HDR10, Dolby Vision and VFR. New profiles
of StaxRip are covered automatically, the inventory comes from StaxRip itself.
"""
import json
import re
import sys
from pathlib import Path

M = '/tmp/media/'

# the media of container/media.sh: frames, size, audio tracks
MEDIA = {
    'h264_aac_subs.mkv': (144, 1280, 720, 1),
    'h264_aac.mp4': (144, 1280, 720, 1),
    'mpeg2_ac3.ts': (144, 1280, 720, 1),
    'dvd.vob': (180, 720, 480, 1),
    'mpeg4_mp3.avi': (144, 1280, 720, 1),
    'flv_h264.flv': (144, 1280, 720, 1),
    'webm_vp9.webm': (144, 1280, 720, 1),
    'hevc10_hdr.mkv': (144, 1280, 720, 2),
    'multi_audio.mkv': (144, 1280, 720, 3),
    'vfr.mkv': (None, 640, 360, 0),
    "name with $pecial 'chars' & (x).mkv": (144, 1280, 720, 1),
    # StaxRip crops the letterbox the level 5 metadata of Dolby Vision describes
    'dv81.mkv': (144, 640, 360, 0),
    'dv10.mkv': (144, 640, 360, 0),
}

HARDWARE = re.compile(r'NVEnc|QSVEnc|VCEEnc|\| (AMD|Intel|Nvidia|VAAPI) \|')

CODECS = {
    'x264Enc': 'h264', 'x265Enc': 'hevc', 'AOMEnc': 'av1', 'Rav1e': 'av1', 'VvencffappEnc': 'vvc', 'BatchEncoder': 'mpeg4',
    'SvtAv1Enc': 'av1', 'SvtAv1EssentialEnc': 'av1', 'SvtAv1HdrEnc': 'av1', 'SvtAv1PsyexEnc': 'av1',
    'SvtAv1TritiumEnc': 'av1',
}

FFMPEG_CODECS = {
    'x264': 'h264', 'x265': 'hevc', 'AOM-AV1': 'av1', 'XviD': 'mpeg4', 'MPEG-4': 'mpeg4', 'Theora': 'theora',
    'ProRes': 'prores', 'ProRes-KS': 'prores', 'R210': 'r210', 'V210': 'v210', 'UT Video': 'utvideo',
    'GoPro CineForm HD': 'cfhd', 'FFV1': 'ffv1', 'VP8': 'vp8', 'VP9': 'vp9',
}

# the slow encoders get their fastest preset, SetSwitch is a function of driver.ps1
FAST = {
    'AOMEnc': "SetSwitch '--cpu-used' 9",
    'Rav1e': '(P).VideoEncoder.Params.Speed.Value = 10',
    'VvencffappEnc': '(P).VideoEncoder.Params.Preset.Value = 1',
    'ffmpegEnc': "SetSwitch '-cpu-used' 8",
}

AUDIO_CODECS = {'AAC': 'aac', 'AC3': 'ac3', 'EAC3': 'eac3', 'FLAC': 'flac', 'MP3': 'mp3', 'Opus': 'opus', 'Vorbis': 'vorbis'}

# the audio formats of the containers, by the extension of the muxer
CONTAINER_AUDIO = {
    'mkv': None, 'nut': None, 'webm': {'Opus', 'Vorbis'}, 'mp4': {'AAC', 'AC3', 'EAC3', 'MP3', 'Opus', 'FLAC'},
    'mov': {'AAC', 'AC3', 'EAC3', 'MP3', 'FLAC'}, 'ismv': {'AAC'}, 'avi': {'MP3', 'AC3'}, 'flv': {'AAC', 'MP3'},
    'ts': {'AAC', 'AC3', 'EAC3', 'MP3', 'Opus'}, 'mpg': {'MP3', 'AC3'}, 'ogg': {'Vorbis', 'Opus', 'FLAC'},
    'asf': {'MP3'}, 'wmv': {'MP3'}, 'mxf': set(), 'ivf': set(),
}


def load(inventory):
    inv = json.loads(Path(inventory).read_text(encoding='utf-8-sig'))
    encoders = [e for e in inv['encoders'] if not HARDWARE.search(e['name'] + ' ' + e['type'])]
    audio = []
    for a in inv['audio']:
        if a['type'] == 'GUIAudioProfile':
            audio += [(a['name'], enc, a['codec']) for enc in a['encoders']]
        elif a['type'] in ('MuxAudioProfile', 'NullAudioProfile'):
            audio.append((a['name'], None, None))
    sources = {engine: [f['name'] for f in inv['filters'][engine] if f['category'] == 'Source'
                        and f['name'] not in ('Manual', 'D2VSource', 'd2vSource')] for engine in inv['filters']}
    return inv, encoders, audio, sources


def codec_of(encoder):
    if encoder['type'] == 'ffmpegEnc':
        return FFMPEG_CODECS.get(encoder['name'].split('|')[-1].strip())
    return CODECS.get(encoder['type'])


def spec(name, source, encoder=None, engine=None, sourcefilter=None, muxer=None, audio=None, code=(), expect=None,
         timeout=600):
    frames, width, height, tracks = MEDIA[source]
    expect = dict(expect or {})
    expect.setdefault('frames', frames)
    expect.setdefault('width', width)
    expect.setdefault('height', height)
    if encoder:
        expect.setdefault('codec', codec_of(encoder))
    if audio and tracks:
        expect.setdefault('audio', [AUDIO_CODECS.get(audio[2])] * tracks if audio[2] and muxer != 'No Muxing' else
                          ([] if audio[0] == 'No Audio' or muxer == 'No Muxing' else None))
    lines = list(code)
    if encoder and encoder['type'] in FAST:
        lines.insert(0, FAST[encoder['type']])
    s = {'name': name, 'sources': [M + source], 'timeout': timeout, 'expect': expect,
         'target': '/work/out/' + re.sub(r'[^A-Za-z0-9]+', '_', name).strip('_') + '.mkv'}
    if engine:
        s['engine'] = engine
    if sourcefilter:
        s['sourcefilter'] = sourcefilter
    if encoder:
        s['video'] = encoder['name']
    if muxer:
        s['muxer'] = muxer
    if audio:
        s['audio'] = audio[0]
        if audio[1]:
            s['audioEncoder'] = audio[1]
    if lines:
        s['code'] = '; '.join(lines)
    return s


def generate(inventory):
    inv, encoders, audio, sources = load(inventory)
    by_name = {e['name']: e for e in encoders}
    x264 = by_name['x264']
    # the default AAC profile chooses the encoder itself
    aac = next((a[0], None, a[2]) for a in audio if a[2] == 'AAC')
    specs = []

    # every encoder with both frame servers, its default muxer
    for e in encoders:
        if e['type'] == 'NullEncoder':
            specs.append(spec(f"encoder {e['name']}", 'h264_aac_subs.mkv', e, audio=aac))
            continue
        ten_bit = e['type'] == 'SvtAv1EssentialEnc'
        for engine in ('AviSynth', 'VapourSynth'):
            if e['type'] == 'BatchEncoder' and engine != 'AviSynth':
                continue
            specs.append(spec(f"encoder {e['name']} ({engine})", 'hevc10_hdr.mkv' if ten_bit else 'h264_aac_subs.mkv',
                              e, engine, audio=aac))

    # every muxer with an encoder and an audio format it supports
    # StaxRip offers the muxers of FFmpeg for every codec, the containers that don't take H.264 get a codec they take
    preference = {'ivf': ['SvtAv1EncApp | SVT-AV1', 'ffmpeg | VP | VP9'], 'webm': ['ffmpeg | VP | VP9'],
                  'ogg': ['ffmpeg | Theora'], 'mxf': ['ffmpeg | ProRes']}
    for m in inv['muxers']:
        if m['type'] == 'BatchMuxer':
            continue
        names = preference.get((m['ext'] or '').lower(), ['x264', 'x265', 'ffmpeg | x264'])
        encoder = next((by_name[n] for n in names if n in by_name), None)
        if not encoder:
            encoder = next((by_name[n] for n in m['encoders'] if n in by_name), None)
        if not encoder:
            continue
        allowed = CONTAINER_AUDIO.get((m['ext'] or '').lower(), None)
        track = next((a for a in audio if a[2] and (allowed is None or a[2] in allowed)), None) if allowed != set() \
            else next(a for a in audio if a[0] == 'No Audio')
        specs.append(spec(f"muxer {m['name']}", 'h264_aac_subs.mkv', encoder, 'AviSynth', muxer=m['name'],
                          audio=track or next(a for a in audio if a[0] == 'No Audio')))

    # every source filter with every source it can open
    for engine, names in sources.items():
        for f in names:
            for source in MEDIA:
                # the L-SMASH source filters open MP4 and MOV only
                if 'SMASH' in f and not source.endswith('.mp4'):
                    continue
                specs.append(spec(f"source {engine} {f} {source}", source, x264, engine, f,
                                  audio=next(a for a in audio if a[0] == 'Copy/Mux')))

    # every audio encoder, the decoders, intermediate formats, downmixing and normalization vary between them
    variants = [
        [],
        ['$t.Decoder = [StaxRip.AudioDecoderMode]::ffmpeg', '$t.DecodingMode = [StaxRip.AudioDecodingMode]::Pipe',
         '$t.Params.ChannelsMode = [StaxRip.ChannelsMode]::_2'],
        ['$t.Decoder = [StaxRip.AudioDecoderMode]::FFAudioSource', '$t.DecodingMode = [StaxRip.AudioDecodingMode]::FLAC',
         '$t.Params.Normalize = $true', '$t.Params.ffmpegNormalizeMode = [StaxRip.ffmpegNormalizeMode]::loudnorm'],
        ['$t.DecodingMode = [StaxRip.AudioDecodingMode]::W64', '$t.Params.Normalize = $true',
         '$t.Params.ffmpegNormalizeMode = [StaxRip.ffmpegNormalizeMode]::volumedetect'],
    ]
    profiles = [(a[0], None, a[2]) for a in audio if a[2]]
    for i, a in enumerate(audio + sorted(set(profiles), key=profiles.index)):
        for k, source in enumerate(('mpeg2_ac3.ts', 'multi_audio.mkv')):
            variant = variants[(2 * i + k) % len(variants)] if a[2] else []
            code = ['foreach ($track in (P).AudioTracks) { $t = $track.AudioProfile; ' + '; '.join(variant) + ' }'] \
                if variant else []
            specs.append(spec(f"audio {a[0]} {a[1] or ('automatic' if a[2] else '')} {source} #{k + 1}".replace('  ', ' '),
                              source, x264, 'AviSynth',
                              audio=a, code=code))

    # features
    svt = by_name['SvtAv1EncApp | SVT-AV1']
    x265 = by_name['x265']
    hdr = by_name['SvtAv1EncApp | SVT-AV1-HDR']
    two_pass = ['(P).VideoEncoder.Params.Mode.Value = 3', '(P).VideoBitrate = 2000']
    specs += [
        spec('feature x264 two pass', 'h264_aac_subs.mkv', x264, 'AviSynth', audio=aac, code=two_pass),
        spec('feature x265 two pass', 'h264_aac_subs.mkv', x265, 'VapourSynth', audio=aac, code=two_pass),
        spec('feature x264 chunks', 'h264_aac_subs.mkv', x264, 'AviSynth', audio=aac,
             code=['(P).VideoEncoder.Params.Chunks.Value = 4']),
        spec('feature SVT-AV1 chunks', 'h264_aac_subs.mkv', svt, 'VapourSynth', audio=aac,
             code=['(P).VideoEncoder.Params.Chunks.Value = 3']),
        spec('feature cut', 'h264_aac_subs.mkv', x264, 'AviSynth', audio=aac, expect={'frames': 72},
             code=['(P).Ranges.Add([StaxRip.Range]::new(24, 71))', '(P).Ranges.Add([StaxRip.Range]::new(96, 119))',
                   '(P).CutFrameCount = (P).Script.GetFrameCount()', '(P).CutFrameRate = (P).Script.GetFramerate()',
                   '$g.UpdateTrim((P).Script)']),
        spec('feature cut VapourSynth', 'multi_audio.mkv', x264, 'VapourSynth', audio=aac, expect={'frames': 48},
             code=['(P).Ranges.Add([StaxRip.Range]::new(48, 95))',
                   '(P).CutFrameCount = (P).Script.GetFrameCount()', '(P).CutFrameRate = (P).Script.GetFramerate()',
                   '$g.UpdateTrim((P).Script)']),
        spec('feature crop and resize', 'h264_aac_subs.mkv', x264, 'AviSynth', audio=aac,
             expect={'width': 640, 'height': 352},
             code=['(P).CropLeft = 16; (P).CropRight = 16; (P).CropTop = 8; (P).CropBottom = 8',
                   "(P).Script.SetFilter('Crop', 'Crop', 'Crop(%crop_left%, %crop_top%, -%crop_right%, -%crop_bottom%)')",
                   '(P).TargetWidth = 640; (P).TargetHeight = 352',
                   "(P).Script.SetFilter('Resize', 'Spline36Resize', 'Spline36Resize(%target_width%, %target_height%)')"]),
        spec('feature deinterlace', 'dvd.vob', x264, 'AviSynth', audio=aac, expect={'frames': 360},
             # NNEDI3, which QTGMC interpolates with by default, only exists for Windows
             code=["(P).Script.SetFilter('Field', 'QTGMC', 'QTGMC(preset=\"Fast\", InputType=0, EdiMode=\"Yadif\")')"]),
        spec('feature deinterlace VapourSynth', 'dvd.vob', x264, 'VapourSynth', audio=aac, expect={'frames': 360},
             code=["(P).Script.SetFilter('Field', 'QTGMC', 'clip = havsfunc.QTGMC(clip, TFF=True, Preset=\"Fast\")')"]),
        spec('feature hard subtitles', 'h264_aac_subs.mkv', x264, 'AviSynth', audio=aac,
             code=["(P).AddHardcodedSubtitleFilter('/tmp/media/en.srt', $true)"]),
        spec('feature hard subtitles VapourSynth', 'h264_aac_subs.mkv', x264, 'VapourSynth', audio=aac,
             code=["(P).AddHardcodedSubtitleFilter('/tmp/media/en.srt', $true)"]),
        spec('feature HDR10 x265', 'hevc10_hdr.mkv', x265, 'VapourSynth', audio=aac, expect={'hdr': True}),
        spec('feature HDR10 SVT-AV1', 'hevc10_hdr.mkv', svt, 'AviSynth', audio=aac, expect={'hdr': True}),
        spec('feature Dolby Vision 8.1 x265', 'dv81.mkv', x265, 'VapourSynth', expect={'dv': True},
             code=['(P).VideoEncoder.Params.VbvBufSize.Value = 20000', '(P).VideoEncoder.Params.VbvMaxRate.Value = 20000']),
        spec('feature Dolby Vision 10 SVT-AV1-HDR', 'dv10.mkv', hdr, 'VapourSynth', expect={'dv': True}),
        spec('feature VFR', 'vfr.mkv', x264, 'AviSynth'),
    ]
    return specs


def write(inventory, folder, shard, match):
    specs = generate(inventory)
    if match:
        specs = [s for s in specs if re.search(match, s['name'])]
    if shard:
        i, n = (int(x) for x in shard.split('/'))
        specs = specs[i - 1::n]
    for k, s in enumerate(specs):
        Path(folder, f'{k:03d}.json').write_text(json.dumps(s, indent=1))
    print(f'{len(specs)} pipelines')


def probe_streams(probe, kind):
    return [s for s in probe.get('streams', []) if s.get('codec_type') == kind]


# the duration of a stream, MKV has it as a tag
def stream_seconds(v):
    if v.get('duration'):
        return float(v['duration'])
    h, m, s = v.get('tags', {}).get('DURATION', '0:0:0').split(':')
    return int(h) * 3600 + int(m) * 60 + float(s)


def check(s, result, probe):
    lines = result.splitlines()
    for key in ('EXCEPTION', 'DIALOG', 'TIMEOUT', 'EXIT'):
        found = [line for line in lines if line.startswith(key)]
        if found:
            return found[0][:1000]
    if 'DONE' not in lines:
        return 'StaxRip ended without finishing the job'
    if not any(line == 'FINISHED: True' or line.startswith('FINISHED: True ') for line in lines):
        return 'the job didn\'t finish'
    if probe is None:
        return 'no target file'
    e = s['expect']
    video = probe_streams(probe, 'video')
    if not video:
        return 'the target has no video'
    v = video[0]
    if e.get('codec') and v.get('codec_name') != e['codec']:
        return f"video codec {v.get('codec_name')}, expected {e['codec']}"
    frames = int(v.get('nb_read_frames') or 0)
    # Theora codes a repeated frame as an empty packet, which decoders skip and MKV drops, its duration counts
    if v.get('codec_name') == 'theora':
        num, den = map(int, v['r_frame_rate'].split('/'))
        frames = round(stream_seconds(v) * num / den)
    if e.get('frames') and abs(frames - e['frames']) > 2:
        return f"{frames} frames, expected {e['frames']}"
    if e.get('width') and (v.get('width'), v.get('height')) != (e['width'], e['height']):
        return f"size {v.get('width')}x{v.get('height')}, expected {e['width']}x{e['height']}"
    if e.get('audio') is not None:
        codecs = [a.get('codec_name') for a in probe_streams(probe, 'audio')]
        if codecs != e['audio']:
            return f"audio {codecs}, expected {e['audio']}"
    if e.get('hdr') and v.get('color_transfer') != 'smpte2084':
        return f"transfer {v.get('color_transfer')}, expected smpte2084 (HDR10)"
    if e.get('dv') and not any('DOVI' in d.get('side_data_type', '') for d in v.get('side_data_list', [])):
        return 'no Dolby Vision configuration in the target'
    return None


def validate(specs, out, results):
    with open(results, 'w') as f:
        for file in sorted(Path(specs).glob('*.json')):
            s = json.loads(file.read_text())
            result_file = Path(out, file.stem + '.result')
            probe_file = Path(out, file.stem + '.probe.json')
            result = result_file.read_text(errors='replace') if result_file.exists() else ''
            probe = json.loads(probe_file.read_text()) if probe_file.exists() and probe_file.stat().st_size else None
            error = check(s, result, probe)
            f.write(f"{'fail' if error else 'pass'}\t{s['name']}\t{error or ''}\n")


def main():
    if len(sys.argv) >= 4 and sys.argv[1] == 'generate':
        options = dict(zip(sys.argv[4::2], sys.argv[5::2]))
        write(sys.argv[2], sys.argv[3], options.get('--shard'), options.get('--match'))
    elif len(sys.argv) == 5 and sys.argv[1] == 'validate':
        validate(sys.argv[2], sys.argv[3], sys.argv[4])
    else:
        print(__doc__, file=sys.stderr)
        return 2
    return 0


if __name__ == '__main__':
    sys.exit(main())
