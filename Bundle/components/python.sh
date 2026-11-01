# relocatable CPython of python-build-standalone, it runs VapourSynth and its Python scripts
version=3.13.16
packages=Python
sources="
https://github.com/astral-sh/python-build-standalone/releases/download/20261013/cpython-3.13.16%2B20261013-x86_64-unknown-linux-gnu-install_only_stripped.tar.gz d0080766b6b74aeb81e1949ef0d9a20e6c17f7b160fb295a69b936ccb17660be cpython-3.13.16-install_only_stripped.tar.gz
https://github.com/astral-sh/python-build-standalone/releases/download/20261013/cpython-3.13.16%2B20261013-x86_64-unknown-linux-gnu-debug-full.tar.zst 29075fa1cb67e4263143da6e3bb43c9eeba2af5bd849c7075944572aa9acc50c cpython-3.13.16-debug-full.tar.zst
"
binary_sources="cpython-3.13.16-install_only_stripped.tar.gz cpython-3.13.16-debug-full.tar.zst"

build() {
    unpack cpython-3.13.16-install_only_stripped.tar.gz
    local lib=python/lib/python3.13
    rm -rf $lib/test $lib/idlelib $lib/tkinter $lib/turtledemo
    # Tk isn't needed, the Berkeley DB of the dbm module has the Sleepycat license
    rm -rf $lib/lib-dynload/_tkinter.* $lib/lib-dynload/_dbm.* python/lib/libtcl* python/lib/tcl* \
        python/lib/tk* python/lib/itcl* python/lib/thread*

    # the license texts of the libraries in Python are only part of the full distributions
    tar -I zstd -xf $C/downloads/cpython-3.13.16-debug-full.tar.zst python/licenses
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
