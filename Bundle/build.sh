#!/bin/sh
# Builds the bundled tools in the build container into Bundle/out, see Bundle/README.md.
# usage: build.sh [<component>...]   without components all are built, cached components are reused
set -eu
cd "$(dirname "$0")"

image=staxriplinux-bundle:$(cat Dockerfile requirements-build.txt | sha256sum | cut -c1-12)

if ! docker image inspect "$image" > /dev/null 2>&1; then
    docker build -t "$image" .
    # the images of former Dockerfile versions
    docker images staxriplinux-bundle --format '{{.Repository}}:{{.Tag}}' | grep -vxF "$image" | xargs -r docker image rm
fi

mkdir -p .cache out
docker run --rm --user "$(id -u):$(id -g)" -e HOME=/tmp -e IMAGE="$image" \
    -v "$PWD:/bundle:ro" -v "$PWD/.cache:/cache" -v "$PWD/out:/out" \
    "$image" /bundle/run.sh "$@"
