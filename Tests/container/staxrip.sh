#!/bin/bash
# Runs StaxRip on Xvfb with the PowerShell scripts of a suite: staxrip.sh <inventory|options|frames|pipelines>
# staxrip.sh script <file> [<arg>...] runs another script, with the media in /tmp/media, to debug a test
SUITE=$1
. /t/common.sh
export HOME=/tmp/home DISPLAY=:99 STAXRIP_SETTINGS=/tmp/home/settings NO_AT_BRIDGE=1
mkdir -p $HOME
Xvfb :99 -screen 0 1600x1000x24 -nolisten tcp > /tmp/xvfb.log 2>&1 &
sleep 1

# staxrip <seconds> <script> [<arg>...]: runs a PowerShell script in a new StaxRip process, the script ends it,
# returns 124 after a timeout
staxrip() {
    local seconds=$1 script=$2 args
    shift 2
    args=$(IFS=';'; echo "$*")
    timeout -k 10 "$seconds" /tmp/app/AppRun "-ExecutePowerShellFile:$script${args:+,$args}" >> /tmp/staxrip.out 2>&1
}

screenshot() {
    xwd -root -display :99 -silent > "/tmp/$1.xwd" 2> /dev/null && $FF -loglevel error -y -i "/tmp/$1.xwd" "/work/out/$1.png"
}

# the first start shows the changelog in a dialog before StaxRip runs the script, it's confirmed with its button
# at the bottom right
rm -f /tmp/started
staxrip 240 /t/started.ps1 /tmp/started &
pid=$!
clicked=

for i in $(seq 1 480); do
    [ -f /tmp/started ] && break
    ids=$(xdotool search --name '^StaxRipLinux v' 2> /dev/null)

    if [ -z "$clicked" ] && [ -n "$ids" ]; then
        best=
        for id in $ids; do
            eval "$(xdotool getwindowgeometry --shell "$id")"
            if [ -z "$best" ] || [ $((WIDTH * HEIGHT)) -lt "$best" ]; then
                best=$((WIDTH * HEIGHT)) bx=$X by=$Y bw=$WIDTH bh=$HEIGHT
            fi
        done
        sleep 1
        xdotool mousemove $((bx + bw - 62)) $((by + bh - 40)) click 1
        clicked=1
    fi

    sleep 0.5
done

wait $pid

if [ ! -f /tmp/started ]; then
    screenshot start
    echo "StaxRip didn't start, see /work/out/start.png" >&2
    tail -20 /tmp/staxrip.out >&2
    exit 1
fi

case $SUITE in
    script)
        cp -r $MEDIA /tmp/media
        shift
        staxrip 900 "$@"
        echo "StaxRip ended with $?" ;;
    inventory)
        staxrip 300 /t/inventory.ps1 /work/inventory.json || { tail -20 /tmp/staxrip.out >&2; exit 1; } ;;
    options)
        : > /work/results/options.tsv
        staxrip 900 /t/options.ps1 /work/results/options.tsv || result fail "options suite" "StaxRip didn't finish" ;;
    frames)
        : > /work/results/frames.tsv
        # the media with the index files StaxRip writes next to them stay, to render scripts again
        rm -rf /work/frames /work/frames-media
        mkdir -p /work/frames
        cp -r $MEDIA /work/frames-media
        # a new process for every source, a project keeps the temp folder and its index files
        for run in "AviSynth frames8.mkv" "AviSynth frames10.mkv" "VapourSynth frames8.mkv" "VapourSynth frames10.mkv"; do
            set -- $run
            staxrip 900 /t/frames.ps1 /work/frames /work/frames-media "$1" "$2" ||
                result fail "frames $1 $2" "StaxRip didn't write the scripts of the filter profiles"
        done
        bash /t/frames.sh /work/frames ;;
    pipelines)
        for spec in /work/specs/*.json; do
            name=$(basename "$spec" .json)
            seconds=$(grep -o '"timeout": *[0-9]*' "$spec" | grep -o '[0-9]*$')
            # StaxRip writes the temp folder next to the source, the media mount is read-only
            rm -rf /tmp/media "$STAXRIP_SETTINGS/Log Files"
            cp -r $MEDIA /tmp/media
            echo "-- $name"

            staxrip $((${seconds:-600} + 60)) /t/driver.ps1 "$spec" "/work/out/$name.result"
            code=$?
            if [ $code != 0 ]; then
                screenshot "$name"
                # timeout returns 124, or 137 when it had to kill StaxRip
                if [ $code = 124 ] || [ $code = 137 ]; then
                    echo "TIMEOUT" >> "/work/out/$name.result"
                else
                    echo "EXIT: $code" >> "/work/out/$name.result"
                fi
            fi

            # StaxRip archives the log and deletes the temp folder of a finished job
            for log in /tmp/media/*_temp/*_staxrip.log "$STAXRIP_SETTINGS/Log Files"/*.log; do
                [ -f "$log" ] && cp "$log" "/work/out/$name.log"
            done
            target=$(sed -n 's/^TARGET: //p' "/work/out/$name.result" 2> /dev/null | tail -1)

            if [ -n "$target" ] && [ -f "$target" ]; then
                $FFPROBE -v error -show_format -show_streams -show_chapters -count_frames -of json "$target" \
                    > "/work/out/$name.probe.json" 2> "/work/out/$name.probe.err"
                rm -f "$target"
            fi
        done ;;
esac

cp /tmp/staxrip.out "/work/out/staxrip-$SUITE.out" 2> /dev/null
