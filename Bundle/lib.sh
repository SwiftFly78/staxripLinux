# Helpers of the recipes in components, sourced by run.sh. A recipe defines:
#   version   upstream version, written to Apps/versions.txt
#   depends   components whose files it needs at build time
#   sources   lines of "<url> <sha256> [<file name>]" or "git <url> <commit> [<file name>]", downloaded and
#             verified before build, git sources are archived with git archive
#   binary_sources  file names of sources that are binaries, they aren't copied to out/sources
#   skip_submodules  names of submodules of its git sources the build doesn't need
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

# runs a download up to three times, servers sometimes drop connections of CI runners for a moment
retry() {
    local i
    for i in 1 2 3; do
        "$@" && return 0
        [ $i = 3 ] || sleep 10
    done
    return 1
}

# archives a commit of a git repository with its submodules, the commit hash verifies the content, the
# submodules are pinned by the commit
git_fetch() {
    local url=$1 commit=$2 file=$3 tmp
    tmp=$(mktemp -d)
    git -C "$tmp" init -q
    retry git -C "$tmp" fetch -q --depth 1 "$url" "$commit"
    if [ "$(git -C "$tmp" rev-parse FETCH_HEAD)" != "$commit" ]; then
        echo "commit mismatch: $url" >&2
        return 1
    fi

    if git -C "$tmp" cat-file -e FETCH_HEAD:.gitmodules 2> /dev/null; then
        git -C "$tmp" checkout -q FETCH_HEAD
        local skip=() s
        for s in ${skip_submodules:-}; do
            skip+=(-c "submodule.$s.update=none")
        done
        # the Bitbucket repositories of sekrit-twc moved to GitHub
        retry git -C "$tmp" -c url.https://github.com/sekrit-twc/.insteadOf=https://bitbucket.org/the-sekrit-twc/ \
            ${skip[@]+"${skip[@]}"} submodule -q update --init --recursive --depth 1
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
            # also TLS and connection errors, which curl doesn't retry by default
            curl -fsSL --retry 5 --retry-all-errors --retry-delay 10 -o "$C/downloads/$file.tmp" "$url"
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
        case " $(echo ${binary_sources:-}) " in
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

# copies the plugins installed with plugin_install to their Apps folders
plugin_bundle() {
    local folder
    for folder in "$@"; do
        mkdir -p "$O/Apps/$folder"
        cp -L "$PREFIX/plugins/$folder"/* "$O/Apps/$folder/"
    done
}

# copies plugin files from the prefix to Apps/Plugins/<VS|AVS|Dual>/<package name>
apps_plugin() {
    local host=$1 package=$2
    shift 2
    apps_install "Plugins/$host/$package" "$@"
}

# stages the license files of the current folder, like LICENSE and COPYING
stage_license_files() {
    local f
    for f in LICENSE* LICENCE* COPYING* License* license* gpl*.txt GPL*; do
        if [ -f "$f" ]; then
            stage_license "$f"
        fi
    done
}

# exports the AviSynth+ entry point of plugins that mark it with __declspec(dllexport), the AviSynth+ headers
# define it as nothing outside Windows, so builds with hidden symbols don't export the entry point
avs_export() {
    sed -i 's/__declspec *(dllexport)/__attribute__((visibility("default")))/' "$@"
}

# builds a plugin with CMake without installing it, with the AviSynth+ headers of the prefix
cmake_plugin() {
    cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_PREFIX_PATH=$PREFIX \
        -DCMAKE_C_FLAGS="$CFLAGS -I$PREFIX/include/avisynth" -DCMAKE_CXX_FLAGS="$CXXFLAGS -I$PREFIX/include/avisynth" "$@"
    cmake --build build
}

# builds a plugin with Meson without installing it, VapourSynth plugins find the headers with the Python
# that has the VapourSynth module
meson_plugin() {
    printf "[binaries]\npython = '%s'\npython3 = '%s'\n" $PREFIX/python/bin/python3 $PREFIX/python/bin/python3 \
        > python.ini
    PATH=$PREFIX/python/bin:$PATH meson setup build --native-file python.ini --buildtype=release --prefix=$PREFIX \
        --libdir=lib "$@"
    PATH=$PREFIX/python/bin:$PATH ninja -C build
}

# builds a plugin with autotools without installing it
autotools_plugin() {
    if [ ! -f configure ]; then
        autoreconf -fi
    fi

    PATH=$PREFIX/python/bin:$PATH ./configure --prefix=$PREFIX "$@"
    make -j$JOBS
}

# builds an AviSynth+ plugin that only has Visual Studio projects into <file>: compiles its sources with the
# SIMD flags their file names ask for, a file that fails gets another try with SSE4.1, and uses the headers of
# AviSynth+ and stand-ins for the parts of the Windows API such plugins use, PORT_USING_STD_MINMAX and
# PORT_MINMAX_MACROS in CXXFLAGS provide the min and max they expect
port_plugin() {
    local out=$1 shim=$PWD/port-shim f flags incs objs=() o
    mkdir -p $shim
    cat > $shim/compat.h << 'EOF'
#pragma once
#include <cstdlib>
#include <cstring>
#include <cstdint>
#include <cstdio>
#include <cmath>
#include <climits>
#include <algorithm>
#define __forceinline inline __attribute__((always_inline))
#define __declspec(x)
#define __stdcall
#define __cdecl
#define __int64 long long
static inline void* _aligned_malloc(size_t size, size_t alignment)
{
    void* p = nullptr;
    return posix_memalign(&p, alignment < sizeof(void*) ? sizeof(void*) : alignment, size) ? nullptr : p;
}
static inline void _aligned_free(void* p) { free(p); }
#if defined(PORT_USING_STD_MINMAX)
using std::min;
using std::max;
#endif
#if defined(PORT_MINMAX_MACROS)
#define min(a, b) (((a) < (b)) ? (a) : (b))
#define max(a, b) (((a) > (b)) ? (a) : (b))
#endif
EOF
    cat > $shim/windows.h << 'EOF'
#pragma once
#include <cstdint>
typedef uint8_t BYTE;
typedef uint16_t WORD;
typedef uint32_t DWORD;
typedef int BOOL;
typedef long LONG;
typedef void* HANDLE;
typedef void* HMODULE;
#define WINAPI
#define TRUE 1
#define FALSE 0
#define OutputDebugString(s) ((void)0)
#define OutputDebugStringA(s) ((void)0)
EOF
    cp $shim/windows.h $shim/Windows.h
    printf '#pragma once\n#include <x86intrin.h>\n' > $shim/intrin.h

    find . \( -name '*.c*' -o -name '*.h*' \) -type f -exec sed -i '/#include/s|\\|/|g' {} +
    find . -path ./port-shim -prune -o -iname avisynth.h -print | xargs -r rm
    find . -type d -name avs -prune -exec rm -r {} +
    incs=$(find . -path ./stage -prune -o -name '*.h' -printf '-I%h\n' | sort -u | tr '\n' ' ')

    for f in $(find . -path ./stage -prune -o \( -name '*.cpp' -o -name '*.c' \) ! -iname 'test*' -print); do
        flags="$CXXFLAGS -fPIC -fpermissive -std=c++17 -include $shim/compat.h"
        [ "${f##*.}" = c ] && flags="$CFLAGS -fPIC"
        flags="$flags $incs -I$PREFIX/include/avisynth -I$shim"

        case "$(basename $f | tr A-Z a-z)" in
            *avx512*) flags="$flags -mavx512f -mavx512bw -mavx512dq -mavx512vl -mavx2 -mfma" ;;
            *avx2*) flags="$flags -mavx2 -mfma" ;;
            *avx*) flags="$flags -mavx" ;;
            *sse4*) flags="$flags -msse4.1" ;;
            *ssse3*) flags="$flags -mssse3" ;;
        esac

        o=$f.o
        g++ $flags -c $f -o $o || g++ $flags -msse4.1 -c $f -o $o
        objs+=($o)
    done

    # AviSynth+ plugins define AVS_linkage, the old headers did it for them
    if ! g++ -shared -Wl,--no-undefined -o $out "${objs[@]}" 2> port-link.log; then
        grep -q 'undefined reference to .AVS_linkage' port-link.log || { cat port-link.log >&2; return 1; }
        printf '#include <avisynth.h>\nconst AVS_Linkage* AVS_linkage = nullptr;\n' > port-linkage.cpp
        g++ $CXXFLAGS -fPIC -I$PREFIX/include/avisynth -c port-linkage.cpp -o port-linkage.o
        g++ -shared -Wl,--no-undefined -o $out "${objs[@]}" port-linkage.o
    fi
}

# prints the compiler flags that make <format> and std::format available, GCC 12 lacks <format>, the
# header-only {fmt} of the fmt component has the same function
std_format_shim() {
    mkdir -p $PWD/std_format
    printf '#pragma once\n#define FMT_HEADER_ONLY\n#include <fmt/format.h>\nnamespace std { using fmt::format; }\n' \
        > $PWD/std_format/format
    echo "-I$PREFIX/share/fmt/include -I$PWD/std_format -include $PWD/std_format/format"
}

# builds the C library of a Rust crate with cargo-c and installs it
cargo_c_install() {
    PATH=$PREFIX/rust/bin:$PATH CARGO_HOME=$PWD/.cargo cargo cinstall --release --locked --library-type=cdylib \
        --prefix=$PREFIX --libdir=$PREFIX/lib --destdir=$DESTDIR
}

# gives the source folder a git repository with a tag, for builds that take their version from git describe
git_version_tag() {
    git init -q
    git add -A
    git -c user.name=bundle -c user.email=bundle@localhost commit -q -m "$1"
    git tag "$1"
}

# installs the built library matching <pattern> as plugins/<Apps folder>/<file name StaxRip looks for>
plugin_install() {
    local f
    f=$(find . -path ./stage -prune -o -type f -iname "$1" -print | head -1)

    if [ -z "$f" ]; then
        echo "no built library matches $1" >&2
        return 1
    fi

    install -D "$f" "$DESTDIR$PREFIX/plugins/$2/$3"
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
