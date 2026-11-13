# relocatable CPython of python-build-standalone, it runs VapourSynth and its Python scripts
version=3.13.18 # renovate: none=set_by_the_update_of_pbs_version
pbs_version=20261026 # renovate: datasource=github-releases depName=astral-sh/python-build-standalone versioning=regex:^(?<major>\d{8})$ source=1,2 special=python-build-standalone
packages=Python
sources="
https://github.com/astral-sh/python-build-standalone/releases/download/20261026/cpython-3.13.18%2B20261026-x86_64-unknown-linux-gnu-install_only_stripped.tar.gz df15fe60d3ff55bf9dcf25824b5572756bb8816ff93865a273d3301d7066a8b7 cpython-3.13.18-install_only_stripped.tar.gz
https://github.com/astral-sh/python-build-standalone/releases/download/20261026/cpython-3.13.18%2B20261026-x86_64-unknown-linux-gnu-debug-full.tar.zst ea42c839fef44ec3179023a5867a022a74ef44a12bcd75d3bbe616444b2439b2 cpython-3.13.18-debug-full.tar.zst
"
binary_sources="cpython-3.13.18-install_only_stripped.tar.gz cpython-3.13.18-debug-full.tar.zst"

build() {
    unpack cpython-3.13.18-install_only_stripped.tar.gz
    local lib=python/lib/python3.13
    rm -rf $lib/test $lib/idlelib $lib/tkinter $lib/turtledemo
    # Tk isn't needed, the Berkeley DB of the dbm module has the Sleepycat license
    rm -rf $lib/lib-dynload/_tkinter.* $lib/lib-dynload/_dbm.* python/lib/libtcl* python/lib/tcl* \
        python/lib/tk* python/lib/itcl* python/lib/thread*

    # the license texts of the libraries in Python are only part of the full distributions
    tar -I zstd -xf $C/downloads/cpython-3.13.18-debug-full.tar.zst python/licenses
    rm python/licenses/LICENSE.{bdb,libX11,libXau,libxcb,tcl,tix}.txt
    stage_license $lib/LICENSE.txt python/licenses/*
    rm -r python/licenses

    mkdir -p $DESTDIR$PREFIX
    mv python $DESTDIR$PREFIX/
}

bundle() {
    mkdir -p $O/Apps/FrameServer
    cp -a $PREFIX/python $O/Apps/FrameServer/Python
}
