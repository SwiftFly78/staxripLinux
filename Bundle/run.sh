#!/bin/bash
# Builds the components inside the build container and assembles the Apps tree, see Bundle/README.md.
# usage: run.sh [<component>...]
set -euo pipefail

export B=/bundle C=/cache O=/out PREFIX=/opt/sr
export PKG_CONFIG_PATH=$PREFIX/lib/pkgconfig:$PREFIX/lib/x86_64-linux-gnu/pkgconfig:$PREFIX/share/pkgconfig
export PATH=$PREFIX/bin:$PATH LD_LIBRARY_PATH=$PREFIX/lib:$PREFIX/lib/x86_64-linux-gnu
export CFLAGS="-O2 -I$PREFIX/include" CXXFLAGS="-O2 -I$PREFIX/include" LDFLAGS="-L$PREFIX/lib"
JOBS=$(nproc)
export JOBS
. $B/lib.sh

mkdir -p $C/downloads $C/components $C/logs
mapfile -t components < <(grep -v '^\s*#' $B/components.list | grep -v '^\s*$')
selected=("$@")

is_selected() {
    [ ${#selected[@]} = 0 ] && return 0
    local i
    for i in "${selected[@]}"; do [ "$i" = "$1" ] && return 0; done
    return 1
}

declare -A keys

# the cache key covers what builds the component: its sources, the build functions of the recipe and
# the helpers, the build image and the keys of the dependencies
component_key() {
    local c=$1 deps d
    deps=$(recipe_var $c depends)
    {
        bash -c '. $B/lib.sh; . $B/components/$1.sh
            unset -f bundle bundle_component apps_install apps_plugin recipe_var
            echo "${version:-}|${depends:-}|${sources:-}"; declare -f' key $c
        echo "$IMAGE"
        for d in $deps; do echo "${keys[$d]}"; done
    } | sha256sum | cut -c1-16
}

for c in "${components[@]}"; do
    keys[$c]=$(component_key $c)
    artifact=$C/components/$c-${keys[$c]}.tar.zst

    if [ ! -f $artifact ]; then
        if ! is_selected $c; then
            echo "== $c isn't built, build it first: build.sh $c" >&2
            exit 1
        fi

        echo "== building $c"
        work=$C/work/$c
        rm -rf $work && mkdir -p $work/stage

        # a separate shell, errexit doesn't apply to commands whose status if tests
        if ! bash -euo pipefail -c '. $B/lib.sh; cd "$1"; build_component "$2"' build $work $c > $C/logs/$c.log 2>&1; then
            tail -40 $C/logs/$c.log >&2
            echo "== $c failed, log: Bundle/.cache/logs/$c.log" >&2
            exit 1
        fi

        mkdir -p $work/stage$PREFIX
        tar -C $work/stage$PREFIX -c . | zstd -q -T0 > $artifact.tmp
        mv $artifact.tmp $artifact
        rm -rf $work
    fi

    # the prefix is rebuilt from the artifacts on every run, so it only contains what the recipes install
    zstd -dc $artifact | tar -C $PREFIX -x --no-overwrite-dir
done

echo "== assembling the Apps tree"
rm -rf $O/Apps $O/sources
mkdir -p $O/Apps/lib $O/Apps/Licenses $O/sources
: > $O/Apps/versions.txt
cat > $O/Apps/Licenses/README.txt << EOF
Tools and libraries bundled with StaxRipLinux, their licenses are in the folders of the same
name. Every release publishes their source code next to the AppImage.

EOF

for c in "${components[@]}"; do
    (bundle_component $c)
done

mkdir -p $C/downloads/ubuntu
python3 $B/collect.py $O/Apps $PREFIX $O/sources $C/downloads $B/excludelist $B/excludelist.local

# the artifacts and downloads of former recipe versions, so the cache doesn't grow
declare -A current

for c in "${components[@]}"; do
    current[components/$c-${keys[$c]}.tar.zst]=1
    for f in $(bash -c '. $B/lib.sh; . $B/components/$1.sh; source_files' files $c); do
        current[downloads/$f]=1
    done
done

for f in $C/components/* $C/downloads/*; do
    if [ -f "$f" ] && [ -z "${current[${f#$C/}]:-}" ]; then
        rm "$f"
    fi
done

echo "== done: $(du -sh $O/Apps | cut -f1) in Bundle/out/Apps"
