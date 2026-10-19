#!/bin/sh
# Tags the commits of the Linux port with their versions, see "Versioning" in Docs/Introduction/Linux.md.
# usage: version.sh [-n] [<commit>]   tags the untagged commits up to <commit> (default HEAD), -n only prints them
#        version.sh current [<commit>]
set -eu

usage() {
    echo "usage: $0 [-n] [<commit>]" >&2
    echo "       $0 current [<commit>]" >&2
    exit 2
}

die() {
    echo "$*" >&2
    exit 1
}

dry_run=false
show_current=false

case ${1:-} in
    -n) dry_run=true; shift ;;
    current) show_current=true; shift ;;
    -*) usage ;;
esac

[ $# -le 1 ] || usage
name=${1:-HEAD}

cd "$(dirname "$0")"

# node-semver, the Semantic Versioning implementation of npm
if [ "$(semver -i patch 1.0.0 2> /dev/null)" = 1.0.1 ]; then
    semver=semver
elif command -v npx > /dev/null 2>&1; then
    semver="npx --yes --loglevel=warn semver@7"
else
    die "semver is missing, install the package semver (Arch Linux) or node-semver (Debian, Ubuntu), or Node.js"
fi

commit=$(git rev-parse -q --verify "$name^{commit}") || die "$name is not a commit"
root=$(git rev-parse -q --verify "refs/tags/v1.0.0^{commit}") ||
    die "The tag v1.0.0 of the first commit of the Linux port is missing, fetch the tags with: git fetch --tags <remote>"
git merge-base --is-ancestor "$root" "$commit" || die "$name is not a commit of the Linux port"

# the tags of StaxRip itself (v2.52.6 and older) don't contain the first commit of the port
versions() {
    tags=$(git tag --list 'v*' --contains "$root" "$@")
    [ -z "$tags" ] || $semver $tags || true
}

# Conventional Commits, unlike in the specification all types bump at least the patch version
bump_of() {
    subject=$(git log -1 --format=%s "$1")
    type=$(printf '%s\n' "$subject" | sed -n -E 's/^([A-Za-z]+)(\([^()]+\))?!?: [^ ].*/\1/p' | tr '[:upper:]' '[:lower:]')

    case $type in
        build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test) ;;
        *)
            # merge and revert commits with the messages of git
            if [ -n "$(git rev-parse -q --verify "$1^2")" ] || [ "${subject#Revert \"}" != "$subject" ]; then
                echo patch
            fi
            return ;;
    esac

    if printf '%s\n' "$subject" | grep -q -E '^[A-Za-z]+(\([^()]+\))?!: ' ||
        git log -1 --format=%B "$1" | grep -q -E '^BREAKING[ -]CHANGE: '; then
        echo major
    elif [ "$type" = feat ]; then
        echo minor
    else
        echo patch
    fi
}

if $show_current; then
    current=$(versions --merged "$commit" | tail -n 1)
    echo "v$current"
    [ -n "$(versions --points-at "$commit")" ] || echo "$name has no version tag yet, v$current is the version of an older commit" >&2
    exit 0
fi

pending=

for c in $(git rev-list --reverse --topo-order --ancestry-path "$root..$commit"); do
    [ -n "$(versions --points-at "$c")" ] || pending="$pending $c"
done

if [ -z "$pending" ]; then
    echo "All commits up to $name have a version, the latest one is v$(versions --merged "$commit" | tail -n 1)"
    exit 0
fi

for c in $pending; do
    [ -n "$(bump_of "$c")" ] ||
        die "The message of $(git log -1 --format='%h "%s"' "$c") doesn't follow Conventional Commits (like \"fix: ...\"), so its version is unknown"
done

# new versions follow the latest version of all branches
latest=$(versions | tail -n 1)
created=

for c in $pending; do
    latest=$($semver -i "$(bump_of "$c")" "$latest")
    tag=v$latest

    if git rev-parse -q --verify "refs/tags/$tag" > /dev/null; then
        die "The tag $tag exists already outside of the Linux port, for example as a release of StaxRip itself"
    fi

    if $dry_run; then
        echo "$tag $(git log -1 --format='%h %s' "$c")"
    else
        git tag -a "$tag" -m "$tag: $(git log -1 --format=%s "$c")" "$c"
        echo "Tagged $(git log -1 --format='%h "%s"' "$c") as $tag"
        created="$created $tag"
    fi
done

if [ -n "$created" ]; then
    remote=$(git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2> /dev/null | cut -d / -f 1) || true
    echo "Push the tags with: git push ${remote:-<remote>}$created"
fi
