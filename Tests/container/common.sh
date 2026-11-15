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

# staxrip <seconds> <script> [<arg>...]: runs a PowerShell script in a new StaxRip process, the script ends it,
# returns 124 after a timeout
staxrip() {
    local seconds=$1 script=$2 args
    shift 2
    args=$(IFS=';'; echo "$*")
    timeout -k 10 "$seconds" /tmp/app/AppRun "-ExecutePowerShellFile:$script${args:+,$args}" >> /tmp/staxrip.out 2>&1
}

# the first start shows the changelog in a dialog before StaxRip runs the script. The OK button at its bottom right
# is clicked when the dialog is visible and keeps its place, and again every 5 seconds while it stays open.
first_start() {
    local pid i=0 next=0 dialog last= best id x y w h
    rm -f /tmp/started
    staxrip 120 /t/started.ps1 /tmp/started &
    pid=$!

    while [ ! -f /tmp/started ] && kill -0 $pid 2> /dev/null; do
        dialog= best=
        for id in $(xdotool search --onlyvisible --name '^StaxRipLinux v' 2> /dev/null); do
            eval "$(xdotool getwindowgeometry --shell "$id")"
            if [ -z "$best" ] || [ $((WIDTH * HEIGHT)) -lt "$best" ]; then
                best=$((WIDTH * HEIGHT)) dialog="$X $Y $WIDTH $HEIGHT"
            fi
        done

        if [ -n "$dialog" ] && [ "$dialog" = "$last" ] && [ $i -ge $next ]; then
            read -r x y w h <<< "$dialog"
            xdotool mousemove $((x + w - 62)) $((y + h - 40)) click 1
            next=$((i + 10))
        fi

        last=$dialog
        i=$((i + 1))
        sleep 0.5
    done

    wait $pid
    [ -f /tmp/started ]
}
