# AviSynth+ with the plugins of its repository, the patch makes it load the plugins of the avisynth folder next
# to the library, so they are found wherever the AppImage is mounted
version=3.7.6
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
    # for FFmpeg, which loads libavisynth.so
    ln -s ../FrameServer/AviSynth/$soname $O/Apps/lib/libavisynth.so
}
