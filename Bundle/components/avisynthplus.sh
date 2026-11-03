# AviSynth+ with the plugins of its repository, the patch makes it load the plugins of the avisynth folder next
# to the library, so they are found wherever the AppImage is mounted
version=3.7.6 # renovate: datasource=github-tags depName=AviSynth/AviSynthPlus extractVersion=^v(?<version>.+)$
# the commit count of the source commit like git rev-list --count, the archive lacks the history, renovate.py updates it
revision=4909
packages=AviSynth
sources="
git https://github.com/AviSynth/AviSynthPlus.git 523370a39933e2f9c208a5d74c1d35d43d177a76
"

build() {
    unpack AviSynthPlus-523370a39933.tar
    cd AviSynthPlus-523370a39933
    git apply << 'EOF'
--- a/avs_core/core/avisynth.cpp
+++ b/avs_core/core/avisynth.cpp
@@ -61,6 +61,7 @@
     #include <objbase.h>
 #else
     #include "avisynth_conf.h"
+    #include <dlfcn.h>
 #if defined(AVS_MACOS)
     #include <mach/host_info.h>
     #include <mach/mach_host.h>
@@ -2684,6 +2685,20 @@ ScriptEnvironment::ScriptEnvironment()
     }
 
     plugin_manager->AddAutoloadDir(system_avs_plugindir, false);
+
+    // the avisynth folder next to the library, for installations that can be moved
+    Dl_info library_info;
+
+    if (dladdr((void*)&CreateScriptEnvironment, &library_info) && library_info.dli_fname) {
+        char* library_path = realpath(library_info.dli_fname, nullptr);
+
+        if (library_path) {
+            std::string library_dir = library_path;
+            free(library_path);
+            library_dir.erase(library_dir.find_last_of('/') + 1);
+            plugin_manager->AddAutoloadDir(library_dir + "avisynth", false);
+        }
+    }
 #endif
 
     top_frame.Set("LOG_ERROR", (int)LOGLEVEL_ERROR);
EOF
    # the version string of builds from git, scripts like CropResize read the revision of it
    sed -i 's/ -DRELEASE_TARBALL//' avs_core/CMakeLists.txt
    sed -i -e "s/@AVS_SEQREV@/$revision/" -e 's/@AVS_BRANCH@/HEAD/' -e 's/@AVS_DEVNEXT_REV@/0/' \
        -e 's/@AVS_DEV_REVDATE@//' -e 's/@AVS_DEV_GITHASH@//' avs_core/core/version.h.in
    cmake_build -DENABLE_PLUGINS=ON
    stage_license distrib/gpl.txt
}

bundle() {
    local dir=$O/Apps/FrameServer/AviSynth soname
    soname=$(patchelf --print-soname $PREFIX/lib/libavisynth.so)
    mkdir -p $dir/avisynth
    cp -L $PREFIX/lib/libavisynth.so $dir/$soname
    ln -s $soname $dir/libavisynth.so
    cp $PREFIX/lib/avisynth/*.so $dir/avisynth/
    # the color constants like color_black, AviSynth+ loads the scripts of the folder
    tar -xOf $C/downloads/AviSynthPlus-523370a39933.tar AviSynthPlus-523370a39933/distrib/ColorPresets/colors_rgb.avsi \
        > $dir/avisynth/colors_rgb.avsi
    # for FFmpeg, which loads libavisynth.so
    ln -s ../FrameServer/AviSynth/$soname $O/Apps/lib/libavisynth.so
}
