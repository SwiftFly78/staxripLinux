#!/bin/bash
# Renders 3 frames of every script frames.ps1 wrote, in parallel: frames.sh <folder>
SUITE=frames
. /t/common.sh
dir=$1

render() {
    local file=$1 name=$2 out
    if [ "${file##*.}" = avs ]; then
        out=$(timeout 300 $AVS2YUV "$file" -frames 3 -o /dev/null 2>&1)
    else
        out=$(timeout 300 $VSPIPE -e 2 "$file" -- 2>&1)
    fi
    local code=$?

    if [ $code = 0 ] && ! printf '%s' "$out" | grep -q '^Error\|Python exception'; then
        printf 'pass\t%s\t\n' "$name"
    else
        printf '%s\n' "$out" > "$file.err"
        [ $code = 124 ] && out="no frames after 300 seconds"
        out=$(printf '%s\n' "$out" |
            grep -v '^\s*$\|^\[.*%\]\|^Progress\|^Avs2YUV\|^Script\|^Output [0-9]* frames\|^Warning: Core freed')
        # the last error with the lines that locate it, or the last lines
        if printf '%s\n' "$out" | grep -q Error; then
            out=$(printf '%s\n' "$out" | tac | sed '/Error/q' | tac | head -3)
        else
            out=$(printf '%s\n' "$out" | tail -3)
        fi
        printf 'fail\t%s\t%s\n' "$name" "$(printf '%s' "$out" | tr '\t\n' '  ' | cut -c1-500)"
    fi
}
export -f render
export AVS2YUV VSPIPE

while IFS=$'\t' read -r file name error; do
    if [ -z "$file" ]; then
        result fail "$name" "$error"
    else
        printf '%s\0%s\0' "$file" "$name"
    fi
done < "$dir/index.tsv" | xargs -0 -n 2 -P "$(nproc)" bash -c 'render "$0" "$1"' >> /work/results/frames.tsv

# the filters of plugins the bundle doesn't have, Windows only plugins for example, are skipped
$PYTHON -I /t/unbundled.py /work/inventory.json /work/results/frames.tsv "$dir/index.tsv"

echo "$(grep -c '^pass' /work/results/frames.tsv) of $(wc -l < /work/results/frames.tsv) scripts render"
