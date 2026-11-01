# the official wheel contains the core, vspipe and VSScript, it's installed into the bundled Python
version=R81
packages="
VapourSynth
vspipe
"
depends="python"
sources="
https://files.pythonhosted.org/packages/97/47/0d0cf0d1028e7cb7a15ea888161e2c7ceaaccaf8c24c3d276f0f7fa8e946/vapoursynth-81-cp312-abi3-manylinux_2_27_x86_64.manylinux_2_28_x86_64.whl 2e3429524728889328897945564235440910a032ebbc879620ba3ff900758b52
https://files.pythonhosted.org/packages/69/6a/f9441173c91a3355a32b870522726ed2252cd3c7b04b45e499f30b1a2c98/vapoursynth-81.tar.gz 3bf6c90ad737e8ffc5605b05a1ff29cd884f8632fbe0eb49a2705942073a96be
"
binary_sources="vapoursynth-81-cp312-abi3-manylinux_2_27_x86_64.manylinux_2_28_x86_64.whl"

build() {
    $PREFIX/python/bin/python3 -m pip install --no-deps --no-index --no-compile --root "$DESTDIR" \
        --no-warn-script-location "$C/downloads/vapoursynth-81-cp312-abi3-manylinux_2_27_x86_64.manylinux_2_28_x86_64.whl"
    local site=$DESTDIR$PREFIX/python/lib/python3.13/site-packages
    # the scripts call the Python of the build, StaxRip starts vspipe of the module directly
    rm -f $DESTDIR$PREFIX/python/bin/vspipe $DESTDIR$PREFIX/python/bin/vapoursynth
    # StaxRip finds the core by its unversioned name
    ln -s libvapoursynth.so.4 $site/vapoursynth/libvapoursynth.so
    stage_license $site/vapoursynth-81.dist-info/licenses/COPYING.LESSER
}
