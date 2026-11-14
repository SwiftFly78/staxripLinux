#!/bin/bash
# Runs StaxRip on Xvfb with the PowerShell scripts of a suite: staxrip.sh <inventory|options|frames|pipelines>
# staxrip.sh script <file> [<arg>...] runs another script, with the media in /tmp/media, to debug a test
SUITE=$1
. /t/common.sh
export HOME=/tmp/home DISPLAY=:99 STAXRIP_SETTINGS=/tmp/home/settings NO_AT_BRIDGE=1
mkdir -p $HOME
Xvfb :99 -screen 0 1600x1000x24 -nolisten tcp > /tmp/xvfb.log 2>&1 &
# GTK can't start before the display accepts connections
for i in $(seq 1 100); do
    xdotool getdisplaygeometry > /dev/null 2>&1 && break
    sleep 0.1
done

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

# a start that doesn't reach the script is tried once more
for attempt in 1 2; do
    first_start && break
    screenshot start-$attempt
    echo "StaxRip didn't start, see /work/out/start-$attempt.png" >&2
    tail -20 /tmp/staxrip.out >&2
    [ $attempt = 2 ] && exit 1
done

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
            # the image sequences of the ffmpeg Image profiles are named <target>_000001.png and so on
            elif [ -n "$target" ] && [ -f "${target%.*}_000001.${target##*.}" ]; then
                $FFPROBE -v error -f image2 -i "${target%.*}_%06d.${target##*.}" -show_format -show_streams \
                    -count_frames -of json > "/work/out/$name.probe.json" 2> "/work/out/$name.probe.err"
                rm -f "${target%.*}"_[0-9][0-9][0-9][0-9][0-9][0-9]."${target##*.}"
            fi
        done ;;
esac

cp /tmp/staxrip.out "/work/out/staxrip-$SUITE.out" 2> /dev/null
