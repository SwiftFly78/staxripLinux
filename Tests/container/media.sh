#!/bin/bash
# Generates the test media with the bundled tools into /work/media, 6 seconds at 24 fps (144 frames) each
SUITE=media
. /t/common.sh
set -e

MKVMERGE=$A/Support/MKVToolNix/mkvmerge
X265=$A/Encoders/x265/x265
DOVI=$A/Support/DoVi_Tool/dovi_tool
SVTHDR=$A/Encoders/SvtAv1EncApp-Hdr/SvtAv1EncApp
mkdir -p $MEDIA /tmp/media
cd /tmp/media

F="$FF -hide_banner -loglevel error -y"
V="-f lavfi -i testsrc2=size=1280x720:rate=24:duration=6"
A1="-f lavfi -i sine=frequency=440:sample_rate=48000:duration=6"
A2="-f lavfi -i sine=frequency=880:sample_rate=48000:duration=6"
MASTER="G(13250,34500)B(7500,3000)R(34000,16000)WP(15635,16450)L(10000000,1)"

printf '1\n00:00:00,500 --> 00:00:02,000\nHello world\n\n2\n00:00:03,000 --> 00:00:05,000\nSecond line\n' > en.srt
printf '1\n00:00:00,500 --> 00:00:02,000\nHallo Welt\n\n2\n00:00:03,000 --> 00:00:05,000\nZweite Zeile\n' > de.srt
printf 'CHAPTER01=00:00:00.000\nCHAPTER01NAME=Intro\nCHAPTER02=00:00:03.000\nCHAPTER02NAME=Main\n' > chapters.txt

# an identity 3D LUT for the Cube filters
{
    echo "LUT_3D_SIZE 2"
    for b in 0 1; do for g in 0 1; do for r in 0 1; do echo "$r.0 $g.0 $b.0"; done; done; done
} > identity.cube

$F $V $A1 -filter_complex "[1]aformat=channel_layouts=stereo[a]" -map 0 -map "[a]" -c:v libx264 -preset ultrafast \
    -pix_fmt yuv420p -c:a aac -b:a 128k base.mkv
$MKVMERGE -q -o h264_aac_subs.mkv --language 1:en --track-name 1:Stereo base.mkv --language 0:en en.srt \
    --language 0:de --default-track 0:no de.srt --chapters chapters.txt
$F -i h264_aac_subs.mkv -map 0:v -map 0:a -map 0:s:0 -c copy -c:s mov_text h264_aac.mp4
$F $V $A1 -filter_complex "[1]aformat=channel_layouts=5.1[a]" -map 0 -map "[a]" -c:v mpeg2video -b:v 6M -c:a ac3 \
    -b:a 384k -f mpegts mpeg2_ac3.ts
$F -f lavfi -i testsrc2=size=720x480:rate=30000/1001:duration=6 $A1 -map 0 -map 1 -vf setfield=tff -c:v mpeg2video \
    -b:v 5M -flags +ilme+ildct -aspect 16:9 -c:a ac3 -b:a 192k -f vob dvd.vob
$F $V $A1 -map 0 -map 1 -c:v mpeg4 -q:v 4 -c:a libmp3lame -b:a 128k mpeg4_mp3.avi
$F $V $A1 -map 0 -map 1 -c:v libx264 -preset ultrafast -pix_fmt yuv420p -c:a aac flv_h264.flv
$F $V $A1 -map 0 -map 1 -c:v libvpx-vp9 -deadline realtime -cpu-used 8 -b:v 1M -c:a libopus webm_vp9.webm
$F $V $A1 $A2 -map 0 -map 1 -map 2 -c:v libx265 -preset ultrafast -pix_fmt yuv420p10le \
    -x265-params "log-level=error:colorprim=bt2020:transfer=smpte2084:colormatrix=bt2020nc:master-display=$MASTER:max-cll=1000,400:hdr10=1" \
    -c:a:0 eac3 -c:a:1 ac3 hevc10_hdr.mkv
$F $V $A1 $A2 $A1 -map 0 -map 1 -map 2 -map 3 -c:v libx264 -preset ultrafast -pix_fmt yuv420p -c:a:0 aac -c:a:1 ac3 \
    -c:a:2 flac -metadata:s:a:0 language=eng -metadata:s:a:0 title=Main -metadata:s:a:1 language=deu \
    -metadata:s:a:2 language=fra multi_audio.mkv
$F -f lavfi -i testsrc2=size=640x360:rate=24:duration=3 -f lavfi -i testsrc2=size=640x360:rate=60:duration=3 \
    -filter_complex "[0][1]concat=n=2:v=1:a=0[v]" -map "[v]" -fps_mode vfr -c:v libx264 -preset ultrafast vfr_tmp.mkv
$MKVMERGE -q -o vfr.mkv vfr_tmp.mkv
$F $A1 -c:a pcm_s16le audio.wav
$F -f lavfi -i sine=frequency=440:sample_rate=44100:duration=6 -af aformat=channel_layouts=stereo -c:a flac audio.flac
cp h264_aac_subs.mkv "name with \$pecial 'chars' & (x).mkv"

# small lossless clips for the frame tests of the filters, 8 and 10 bit
$F -f lavfi -i testsrc2=size=640x360:rate=24:duration=2 -c:v ffv1 -pix_fmt yuv420p -color_primaries bt709 \
    -color_trc bt709 -colorspace bt709 -color_range tv frames8.mkv
$F -f lavfi -i testsrc2=size=640x360:rate=24:duration=2 -c:v ffv1 -pix_fmt yuv420p10le -color_primaries bt2020 \
    -color_trc smpte2084 -colorspace bt2020nc -color_range tv frames10.mkv

# Dolby Vision profile 8.1 (HEVC) and 10 (AV1) with the RPUs dovi_tool generates, letterboxed like films
cat > dv.json << 'EOF'
{
  "cm_version": "V40",
  "profile": "8.1",
  "length": 144,
  "level5": {"active_area_left_offset": 0, "active_area_right_offset": 0, "active_area_top_offset": 40, "active_area_bottom_offset": 40},
  "level6": {"max_display_mastering_luminance": 1000, "min_display_mastering_luminance": 1, "max_content_light_level": 1000, "max_frame_average_light_level": 400}
}
EOF
$DOVI generate -j dv.json -o dv.rpu > /dev/null
$F -f lavfi -i testsrc2=size=640x360:rate=24:duration=6 -vf pad=640:440:0:40 -pix_fmt yuv420p10le -strict -1 dv.y4m
$X265 --log-level error --y4m --input dv.y4m --profile main10 --output-depth 10 --preset ultrafast \
    --dolby-vision-profile 8.1 --dolby-vision-rpu dv.rpu --vbv-bufsize 20000 --vbv-maxrate 20000 --colorprim bt2020 \
    --transfer smpte2084 --colormatrix bt2020nc --master-display "$MASTER" --max-cll 1000,400 --hdr10 --repeat-headers \
    -o dv81.hevc
$MKVMERGE -q -o dv81.mkv dv81.hevc
$SVTHDR -i dv.y4m --preset 10 --dolby-vision-rpu dv.rpu --color-primaries 9 --transfer-characteristics 16 \
    --matrix-coefficients 9 --mastering-display "G(0.17,0.797)B(0.131,0.046)R(0.708,0.292)WP(0.3127,0.329)L(1000,0.0001)" \
    --content-light 1000,400 -b dv10.ivf > /dev/null 2>&1
$MKVMERGE -q -o dv10.mkv dv10.ivf

rm -f base.mkv vfr_tmp.mkv dv.y4m dv.rpu dv.json dv81.hevc dv10.ivf
cp -r /tmp/media/. $MEDIA/
echo "$(ls $MEDIA | wc -l) files in $MEDIA"
