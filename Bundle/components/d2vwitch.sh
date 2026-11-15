# D2V Witch, ported to Qt 6, its video preview uses the VapourSynth and d2vsource of the bundle
version=5-f002376 # renovate: datasource=git-refs depName=https://github.com/dubhater/D2VWitch.git branch=master digest=f00237639910eef1a9ee955b3de14d9a8df57908
packages="
D2V Witch
"
depends="qtbase ffmpeg6 vapoursynth"
sources="
git https://github.com/dubhater/D2VWitch.git f00237639910eef1a9ee955b3de14d9a8df57908
"
skip_submodules="ffmpeg"

build() {
    unpack D2VWitch-f00237639910.tar
    cd D2VWitch-f00237639910
    # Qt 6, which needs C++17, and the static FFmpeg 6.1
    sed -i -e 's/qt5/qt6/g' -e 's/cpp_std=c++11/cpp_std=c++17/' \
        -e "s/dependency('\(libav[a-z]*\)')/dependency('\1', static: true)/" meson.build
    sed -i 's/\.midRef(/.mid(/g' src/D2VWitch.cpp
    sed -i 's/static_cast<void (QButtonGroup::\*)(int, bool)>(&QButtonGroup::buttonToggled)/\&QButtonGroup::idToggled/' \
        src/GUIWindow.cpp
    # the video preview: the library of the core, libvapoursynth.so of the bundle is the Python module, d2vsource
    # loaded by path like on Windows when VapourSynth didn't autoload it, and planar RGB, VapourSynth R79 doesn't
    # convert to the packed RGB of API 3 anymore
    sed -i '/^#if defined(_WIN32)$/{N;s/^#if defined(_WIN32)\n\(    \/\/ If d2vsource was not autoloaded\)/#if 1\n\1/}' \
        src/GUIWindow.cpp
    sed -i -e 's/QStringLiteral("libvapoursynth")/QStringLiteral("libvapoursynth.so.4")/' \
        -e 's/"d2vsource\.dll"/getenv("D2VWITCH_D2VSOURCE") ?: "libd2vsource.so"/' src/GUIWindow.cpp
    git apply << 'EOF'
--- a/src/GUIWindow.cpp
+++ b/src/GUIWindow.cpp
@@ -1060,11 +1060,19 @@
         return;
     }

-    const uint8_t *ptr = vsapi->getReadPtr(frame, 0);
+    const uint8_t *r = vsapi->getReadPtr(frame, 0);
+    const uint8_t *g = vsapi->getReadPtr(frame, 1);
+    const uint8_t *b = vsapi->getReadPtr(frame, 2);
     int width = vsapi->getFrameWidth(frame, 0);
     int height = vsapi->getFrameHeight(frame, 0);
     int stride = vsapi->getStride(frame, 0);
-    QPixmap pixmap = QPixmap::fromImage(QImage(ptr, width, height, stride, QImage::Format_RGB32).mirrored(false, true));
+    QImage image(width, height, QImage::Format_RGB32);
+    for (int y = 0; y < height; y++) {
+        QRgb *line = reinterpret_cast<QRgb *>(image.scanLine(y));
+        for (int x = 0; x < width; x++)
+            line[x] = qRgb(r[y * stride + x], g[y * stride + x], b[y * stride + x]);
+    }
+    QPixmap pixmap = QPixmap::fromImage(image);

     video_frame_label->setPixmap(pixmap);
     // Must free the frame only after replacing the pixmap.
@@ -1249,7 +1257,7 @@

     vsapi->propSetNode(args, "clip", vsnode, paReplace);
     vsapi->freeNode(vsnode);
-    vsapi->propSetInt(args, "format", pfCompatBGR32, paReplace);
+    vsapi->propSetInt(args, "format", pfRGB24, paReplace);
     vsapi->propSetData(args, "matrix_in_s", "709", -1, paReplace);
     vsapi->propSetInt(args, "prefer_props", 1, paReplace);

EOF
    PKG_CONFIG_PATH=$PREFIX/ffmpeg6/lib/pkgconfig:$PKG_CONFIG_PATH meson_build
    # the ISC license of the code, MPEGParser.cpp is LGPL 2.1 like FFmpeg
    sed -n '3,16p' src/Bullshit.h > LICENSE
    stage_license LICENSE readme.rst
}

bundle() {
    qt_app_install "Support/D2V Witch" bin/d2vwitch 'apps=$dir/../..
for vs in "$apps"/FrameServer/Python/lib/python3*/site-packages/vapoursynth; do
    [ -d "$vs" ] && export LD_LIBRARY_PATH="$vs${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
done
export D2VWITCH_D2VSOURCE="$apps/Plugins/VS/d2vsource/libd2vsource.so"'
}
