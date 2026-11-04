#!/bin/bash
# Runs the test suites of the full AppImage in containers, see Tests/README.md.
# usage: run.sh <AppImage> <suite>...
#   suites: static, tools, options, frames, pipelines or pipelines:<shard>/<count>, all
# The results are lines of "<pass|fail|skip>\t<test>\t<detail>" in Tests/.work/results/<suite>.tsv,
# report.py summarizes them. PIPELINES_MATCH=<regex> only runs the pipelines whose names match.
set -euo pipefail
cd "$(dirname "$0")"

if [ $# -lt 2 ]; then
    sed -n 3,6p "$0" >&2
    exit 2
fi

appimage=$(realpath "$1")
shift
# downloaded artifacts lose the executable bit
[ -x "$appimage" ] || chmod +x "$appimage"
W=${TEST_WORK:-$PWD/.work}
mkdir -p "$W/results"

# the image of Dockerfile, with gpu=1 the one of Dockerfile.gpu with the software drivers of Vulkan and OpenCL
image() {
    local file=Dockerfile tag
    [ "$1" = 1 ] && file=Dockerfile.gpu
    tag=staxriplinux-test$([ "$1" = 1 ] && echo -gpu || true):$(sha256sum $file | cut -c1-12)

    if ! docker image inspect "$tag" > /dev/null 2>&1; then
        # without context, the images copy no files
        docker build -q -t "$tag" - < $file > /dev/null
    fi

    echo "$tag"
}

# container <gpu> <script> [<arg>...]
container() {
    local gpu=$1
    shift
    docker run --rm --user "$(id -u):$(id -g)" --shm-size=1g -v "$appimage:/app.AppImage:ro" \
        -v "$PWD/container:/t:ro" -v "$W:/work" "$(image "$gpu")" bash "$@"
}

media() {
    [ -f "$W/media/.done" ] && [ "$W/media/.done" -nt "$appimage" ] && return
    rm -rf "$W/media"
    container 0 /t/media.sh
    touch "$W/media/.done"
}

inventory() {
    [ -f "$W/inventory.json" ] && [ "$W/inventory.json" -nt "$appimage" ] && return
    container 0 /t/staxrip.sh inventory
    [ -s "$W/inventory.json" ] || { echo "StaxRip didn't write the inventory" >&2; exit 1; }
}

suites=("$@")
[ "${suites[*]}" = all ] && suites=(static tools options frames pipelines)

for suite in "${suites[@]}"; do
    echo "== $suite"

    case $suite in
        static)
            inventory
            container 0 /t/static.sh ;;
        tools)
            media
            container 0 /t/tools.sh ;;
        options)
            inventory
            container 0 /t/staxrip.sh options ;;
        frames)
            media
            inventory
            container 1 /t/staxrip.sh frames ;;
        pipelines|pipelines:*)
            media
            inventory
            shard=${suite#pipelines}
            shard=${shard#:}
            rm -rf "$W/specs" "$W/out"
            mkdir -p "$W/specs" "$W/out"
            python3 -I pipelines.py generate "$W/inventory.json" "$W/specs" ${shard:+--shard "$shard"} \
                ${PIPELINES_MATCH:+--match "$PIPELINES_MATCH"}
            container 0 /t/staxrip.sh pipelines
            python3 -I pipelines.py validate "$W/specs" "$W/out" "$W/results/pipelines${shard:+-${shard/\//-of-}}.tsv" ;;
        *)
            echo "unknown suite: $suite" >&2
            exit 2 ;;
    esac
done

python3 -I report.py "$W/results"
