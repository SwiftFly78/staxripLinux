# the official wheel contains the core, vspipe and VSScript, it's installed into the bundled Python, R79 is the last
# version that loads plugins of API 3, which many plugins still use, and the version upstream StaxRip ships
version=R79
packages="
VapourSynth
vspipe
"
depends="python"
sources="
https://files.pythonhosted.org/packages/c2/82/a4f5e879ff0bb6722ecbecf01482b0783ed05d42aefa7717e6a6f4f77180/vapoursynth-79-cp312-abi3-manylinux_2_27_x86_64.manylinux_2_28_x86_64.whl 6f1e37f0ed8eb73e61c3c231fd7f7a0f7acfa893e98d026686e9c81e52c9ce06
https://files.pythonhosted.org/packages/2d/4d/bb7fcc7f304e7248cf6c83ec0c3c97ee4b4fa2e05bfbbe2a578a9b41fab9/vapoursynth-79.tar.gz 01311b79ef22334115f79a78a6d7548bd29f35cc903ee0cea12443d49e142eb1
"
binary_sources="vapoursynth-79-cp312-abi3-manylinux_2_27_x86_64.manylinux_2_28_x86_64.whl"

build() {
    $PREFIX/python/bin/python3 -m pip install --no-deps --no-index --no-compile --root "$DESTDIR" \
        --no-warn-script-location "$C/downloads/vapoursynth-79-cp312-abi3-manylinux_2_27_x86_64.manylinux_2_28_x86_64.whl"
    local site=$DESTDIR$PREFIX/python/lib/python3.13/site-packages
    # the core is part of the Python module, StaxRip finds the folder of VapourSynth by the name of the core
    ln -s vapoursynth.abi3.so $site/vapoursynth/libvapoursynth.so
    stage_license $site/vapoursynth-79.dist-info/licenses/COPYING.LESSER

    # the headers of API 4 and 3 for the plugins, which don't link VapourSynth
    unpack vapoursynth-79.tar.gz
    mkdir -p $DESTDIR$PREFIX/include/vapoursynth $DESTDIR$PREFIX/lib/pkgconfig
    cp vapoursynth-79/include/*.h $DESTDIR$PREFIX/include/vapoursynth/
    cat > $DESTDIR$PREFIX/lib/pkgconfig/vapoursynth.pc << PC
prefix=$PREFIX
libdir=\${prefix}/lib
includedir=\${prefix}/include

Name: vapoursynth
Description: A frameserver for the 21st century
Version: 79
Cflags: -I\${includedir}/vapoursynth
Libs:
PC
}

bundle() {
    # the plugin builds use the scripts, they call the Python of the build, StaxRip starts vspipe of the module
    rm $O/Apps/FrameServer/Python/bin/vspipe $O/Apps/FrameServer/Python/bin/vapoursynth
}
