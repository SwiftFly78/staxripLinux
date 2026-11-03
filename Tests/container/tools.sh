#!/bin/bash
# Runs a small real job with every bundled tool and the encoders of FFmpeg that the profiles of StaxRip use
SUITE=tools
. /t/common.sh
: > /work/results/tools.tsv
T=/tmp/tools
export HOME=/tmp/home
mkdir -p $T $HOME
cd $T

MKV=$A/Support/MKVToolNix
F="$FF -hide_banner -loglevel error -y"

# nonempty <test> <file> <command>...: the command has to succeed and write the file
nonempty() {
    local name=$1 file=$2
    shift 2
    check "$name" bash -c '"$@" && test -s "$0"' "$file" "$@"
}

$F -f lavfi -i testsrc2=size=320x240:rate=24:duration=1 -pix_fmt yuv420p in8.y4m
$F -f lavfi -i testsrc2=size=320x240:rate=24:duration=1 -pix_fmt yuv420p10le -strict -1 in10.y4m
$F -f lavfi -i sine=frequency=440:sample_rate=48000:duration=2 -ac 2 in.wav
$F -i in8.y4m -f rawvideo -pix_fmt yuv420p in8.yuv

echo "-- video encoders"
nonempty "x264" out.264 $A/Encoders/x264/x264 --preset ultrafast --frames 8 -o out.264 in8.y4m
nonempty "x265" out.hevc $A/Encoders/x265/x265 --preset ultrafast --frames 8 --input in8.y4m -o out.hevc
nonempty "x265 10-bit" out10.hevc $A/Encoders/x265/x265 --preset ultrafast --frames 8 --output-depth 10 --input in10.y4m -o out10.hevc
for svt in SvtAv1EncApp SvtAv1EncApp-Hdr SvtAv1EncApp-Psyex SvtAv1EncApp-Tritium; do
    nonempty "$svt" $svt.ivf $A/Encoders/$svt/SvtAv1EncApp -i in8.y4m --frames 8 --preset 12 -b $svt.ivf
done
nonempty "SvtAv1EncApp-Essential" essential.ivf $A/Encoders/SvtAv1EncApp-Essential/SvtAv1EncApp -i in10.y4m --frames 8 \
    --preset 10 -b essential.ivf
nonempty "aomenc" aom.ivf $A/Encoders/AOMEnc/aomenc --ivf --limit=8 --cpu-used=9 --rt -o aom.ivf in8.y4m
nonempty "rav1e" rav1e.ivf $A/Encoders/Rav1e/rav1e in8.y4m --limit 8 --speed 10 -y -o rav1e.ivf
nonempty "vvencFFapp" out.266 $A/Encoders/vvencFFapp/vvencFFapp -i in8.y4m --preset faster -f 8 -b out.266
nonempty "xvid_encraw" xvid.m4v $A/Encoders/xvid_encraw/xvid_encraw -i in8.yuv -type 0 -csp i420 -w 320 -h 240 \
    -frames 8 -o xvid.m4v

echo "-- FFmpeg"
for c in libx264 libx265 libaom-av1 libxvid mpeg4 libtheora prores prores_ks r210 v210 utvideo cfhd ffv1 libvpx \
    libvpx-vp9 libsvtav1 mpeg2video; do
    extra=
    case $c in
        libaom-av1) extra="-cpu-used 8 -row-mt 1" ;;
        libsvtav1) extra="-preset 12" ;;
    esac
    nonempty "ffmpeg -c:v $c" "v_$c.mkv" $F -i in8.y4m -frames:v 8 -c:v $c $extra "v_$c.mkv"
done
for c in aac ac3 eac3 flac libmp3lame libopus libvorbis pcm_s24le; do
    nonempty "ffmpeg -c:a $c" "a_$c.mka" $F -i in.wav -c:a $c "a_$c.mka"
done
check "ffmpeg loudnorm" $F -i in.wav -af loudnorm=print_format=summary -f null -
check "ffmpeg rubberband" $F -i in.wav -af rubberband=tempo=1.04 -f null -
for f in h264_aac_subs.mkv mpeg2_ac3.ts dvd.vob mpeg4_mp3.avi webm_vp9.webm hevc10_hdr.mkv dv81.mkv dv10.mkv; do
    check "ffmpeg decode $f" $F -i "$MEDIA/$f" -f null -
done

echo "-- audio encoders"
nonempty "fdkaac" out.m4a $A/Audio/fdkaac/fdkaac -m 4 -o out.m4a in.wav
nonempty "opusenc" out.opus $A/Audio/Opus/opusenc --quiet in.wav out.opus
check "truehdd" $A/Audio/truehdd/truehdd --version

echo "-- muxers and demuxers"
nonempty "mkvmerge" mux.mkv $MKV/mkvmerge -q -o mux.mkv out.264 out.m4a $MEDIA/en.srt --chapters $MEDIA/chapters.txt
nonempty "mkvextract" track0.h264 $MKV/mkvextract tracks mux.mkv 0:track0.h264
check "mkvinfo" $MKV/mkvinfo mux.mkv
check "mkvpropedit" $MKV/mkvpropedit mux.mkv --edit info --set title=Test
nonempty "MP4Box" mux.mp4 $A/Support/MP4Box/MP4Box -quiet -add out.264 -add out.m4a -new mux.mp4
nonempty "MP4Box raw" mux_track1.264 $A/Support/MP4Box/MP4Box -quiet -raw 1 mux.mp4

echo "-- HDR tools"
$MKV/mkvextract tracks $MEDIA/dv81.mkv 0:dv81.hevc > /dev/null
nonempty "dovi_tool extract-rpu" rpu.bin $A/Support/DoVi_Tool/dovi_tool extract-rpu dv81.hevc -o rpu.bin
check "dovi_tool info" $A/Support/DoVi_Tool/dovi_tool info -i rpu.bin -s
check "hdr10plus_tool" $A/Support/HDR10Plus_Tool/hdr10plus_tool --version

echo "-- frame servers"
printf 'BlankClip(length=8, width=320, height=240, pixel_type="YV12")\n' > blank.avs
check "avs2yuv" $AVS2YUV blank.avs -o /dev/null
printf 'import vapoursynth as vs\nvs.core.std.BlankClip(width=320, height=240, format=vs.YUV420P8, length=8).set_output()\n' \
    > blank.vpy
# like StaxRip, which writes the Python of the bundle into the configuration of VSScript
vsscript=$(realpath $A/FrameServer/Python/lib/python3.*/site-packages/vapoursynth/libvsscript.so)
python=$(realpath $A/FrameServer/Python/bin/python3.*[0-9])
mkdir -p $HOME/.config/vapoursynth
printf '"%s" = ["%s","%s","%s"]\n' "$vsscript" "$python" "$(realpath $A/FrameServer/Python/lib/libpython3.*.so.1.0)" \
    "$(stat -c %Y "$python")" > $HOME/.config/vapoursynth/vapoursynth.toml
VSSCRIPT_PATH=$vsscript check "vspipe" $VSPIPE blank.vpy --

echo "-- other tools"
check "7z" bash -c "$A/Support/7zip/7z a -bd archive.7z in.wav > /dev/null && $A/Support/7zip/7z t archive.7z > /dev/null"
# StaxRip downloads the languages
check "tesseract" $A/Subtitles/Tesseract/tesseract --version
check "mpv" $A/Support/mpv.net/mpv --no-config --vo=null --ao=null --frames=24 $MEDIA/h264_aac_subs.mkv

echo "$(grep -c '^pass' /work/results/tools.tsv) of $(wc -l < /work/results/tools.tsv) tool tests pass"
