# Helpers of the recipes in components, sourced by run.sh. A recipe defines:
#   version   upstream version, written to Apps/versions.txt
#   depends   components whose files it needs at build time
#   sources   lines of "<url> <sha256> [<file name>]" or "git <url> <commit> [<file name>]", downloaded and
#             verified before build, git sources are archived with git archive
#   binary_sources  file names of sources that are binaries, they aren't copied to out/sources
#   packages  IDs of the StaxRip packages the component provides, one per line, StaxRip reads their
#             version from Apps/versions.txt
#   build     builds and installs into $DESTDIR$PREFIX, the work folder is the current folder
#   bundle    copies the files of the component from $PREFIX into the Apps tree
# The downloaded sources of every component are copied to out/sources, so the release can provide
# the source code of the bundled tools.

recipe_var() {
    (
        . $B/components/$1.sh
        eval "echo \"\${$2:-}\""
    )
}

# archives a commit of a git repository with its submodules, the commit hash verifies the content, the
# submodules are pinned by the commit
git_fetch() {
    local url=$1 commit=$2 file=$3 tmp
    tmp=$(mktemp -d)
    git -C "$tmp" init -q
    git -C "$tmp" fetch -q --depth 1 "$url" "$commit"
    if [ "$(git -C "$tmp" rev-parse FETCH_HEAD)" != "$commit" ]; then
        echo "commit mismatch: $url" >&2
        return 1
    fi

    if git -C "$tmp" cat-file -e FETCH_HEAD:.gitmodules 2> /dev/null; then
        git -C "$tmp" checkout -q FETCH_HEAD
        # the Bitbucket repositories of sekrit-twc moved to GitHub
        git -C "$tmp" -c url.https://github.com/sekrit-twc/.insteadOf=https://bitbucket.org/the-sekrit-twc/ \
            submodule -q update --init --recursive --depth 1
        tar -C "$tmp" --exclude=.git --transform "s|^\.|${file%.tar}|" -cf "$C/downloads/$file.tmp" .
    else
        git -C "$tmp" archive --prefix="${file%.tar}/" -o "$C/downloads/$file.tmp" FETCH_HEAD
    fi

    mv "$C/downloads/$file.tmp" "$C/downloads/$file"
    rm -rf "$tmp"
}

# downloads and verifies the sources of the recipe that is loaded
fetch() {
    local url sum file
    while read -r url sum file; do
        [ -z "$url" ] && continue

        if [ "$url" = git ]; then
            set -- $sum $file
            file=${3:-$(basename "$1" .git)-${2:0:12}.tar}
            [ -f "$C/downloads/$file" ] || git_fetch "$1" "$2" "$file"
            continue
        fi

        file=${file:-$(basename "${url%%\?*}")}

        if [ ! -f "$C/downloads/$file" ]; then
            curl -fsSL --retry 3 -o "$C/downloads/$file.tmp" "$url"
            mv "$C/downloads/$file.tmp" "$C/downloads/$file"
        fi

        if ! echo "$sum  $C/downloads/$file" | sha256sum -c --quiet; then
            echo "checksum mismatch: $file" >&2
            rm -f "$C/downloads/$file"
            return 1
        fi
    done <<< "$sources"
}

source_files() {
    local url sum file
    while read -r url sum file; do
        [ -z "$url" ] && continue
        if [ "$url" = git ]; then
            set -- $sum $file
            echo "${3:-$(basename "$1" .git)-${2:0:12}.tar}"
        else
            echo "${file:-$(basename "${url%%\?*}")}"
        fi
    done <<< "$sources"
}

# extracts a downloaded file into the current folder
unpack() {
    local f=$C/downloads/$1
    case $1 in
        *.tar.gz|*.tgz|*.tar.xz|*.tar.bz2|*.tar.zst|*.tar) tar -xf "$f" ;;
        *.zip|*.whl) python3 -c 'import sys, zipfile; zipfile.ZipFile(sys.argv[1]).extractall()' "$f" ;;
        *.deb) dpkg-deb -x "$f" . ;;
        *) cp "$f" . ;;
    esac
}

build_component() {
    export name=$1 DESTDIR=$PWD/stage
    . $B/components/$name.sh
    fetch
    build
}

bundle_component() {
    export name=$1
    . $B/components/$name.sh
    echo "$name $version" >> $O/Apps/Licenses/README.txt

    local p
    while read -r p; do
        [ -n "$p" ] && echo "$p = $version" >> $O/Apps/versions.txt
    done <<< "${packages:-}"

    if declare -F bundle > /dev/null; then
        bundle
    fi

    if [ -d $PREFIX/share/licenses/$name ]; then
        cp -r $PREFIX/share/licenses/$name $O/Apps/Licenses/
    fi

    local f
    for f in $(source_files); do
        case " ${binary_sources:-} " in
            *" $f "*) ;;
            *) cp "$C/downloads/$f" $O/sources/ ;;
        esac
    done
}

# copies license files from the work folder into the prefix, bundle_component ships them,
# <file>:<name> stores a file under another name
stage_license() {
    mkdir -p $DESTDIR$PREFIX/share/licenses/$name
    local f
    for f in "$@"; do
        cp "${f%%:*}" "$DESTDIR$PREFIX/share/licenses/$name/$(basename "${f#*:}")"
    done
}

# copies files from the prefix to Apps/<location>
apps_install() {
    local location=$1
    shift
    mkdir -p "$O/Apps/$location"
    local f
    for f in "$@"; do
        cp -L "$PREFIX/$f" "$O/Apps/$location/"
    done
}

# copies plugin files from the prefix to Apps/Plugins/<VS|AVS|Dual>/<package name>
apps_plugin() {
    local host=$1 package=$2
    shift 2
    apps_install "Plugins/$host/$package" "$@"
}

configure_make() {
    ./configure --prefix=$PREFIX "$@"
    make -j$JOBS
    make install
}

cmake_build() {
    cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=$PREFIX \
        -DCMAKE_INSTALL_LIBDIR=lib -DCMAKE_PREFIX_PATH=$PREFIX "$@"
    cmake --build build
    cmake --install build
}

meson_build() {
    meson setup build --prefix=$PREFIX --libdir=lib --buildtype=release "$@"
    ninja -C build
    ninja -C build install
}
