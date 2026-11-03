# Sourced by the suites in the test container: extracts the AppImage once, defines the paths of the bundled tools and
# records results in /work/results/<suite>.tsv as lines of "<pass|fail|skip>\t<test>\t<detail>".
set -uo pipefail

if [ ! -d /tmp/app ]; then
    (cd /tmp && /app.AppImage --appimage-extract > /dev/null && mv squashfs-root app) || exit 1
fi

A=/tmp/app/usr/lib/staxrip/Apps
FF=$A/Encoders/ffmpeg/ffmpeg
FFPROBE=$A/Encoders/ffmpeg/ffprobe
AVS2YUV=$A/FrameServer/AviSynth/avs2yuv
PYTHON=$A/FrameServer/Python/bin/python3
VSPIPE=$(ls $A/FrameServer/Python/lib/python3.*/site-packages/vapoursynth/vspipe 2> /dev/null | head -1)
MEDIA=/work/media
mkdir -p /work/results /work/out

result() {
    printf '%s\t%s\t%s\n' "$1" "$2" "$(printf '%s' "${3:-}" | tr '\t\n' '  ' | cut -c1-500)" >> /work/results/$SUITE.tsv
}

# check <test> <command>...: records pass or fail with the last lines of the output
check() {
    local name=$1 out
    shift
    if out=$(timeout 300 "$@" 2>&1); then
        result pass "$name"
    else
        result fail "$name" "$(printf '%s\n' "$out" | grep -v '^\s*$' | tail -3)"
    fi
}
