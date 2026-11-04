version=0.3.1 # renovate: datasource=github-tags depName=Asd-g/VapourSource
packages="
VapourSource avs
"
depends="avisynthplus vapoursynth fmt"
sources="
git https://github.com/Asd-g/VapourSource.git 4d2f765042d2839489c6193eda9b8222c8b5623e
"

build() {
    unpack VapourSource-4d2f765042d2.tar
    cd VapourSource-4d2f765042d2
    # VSScript of VSSCRIPT_PATH, which StaxRipLinux sets for the bundled VapourSynth, or of the system, AviSynth+
    # passes UTF-8 on Linux
    sed -i -e 's|#include <windows.h>|#include <dlfcn.h>\n#include <cstdlib>|' \
        -e '/struct vsscript_loader/i\    void* open_vsscript() noexcept\n    {\n        const char* names[] = {std::getenv("VSSCRIPT_PATH"), "libvapoursynth-script.so.0", "libvapoursynth-script.so"};\n        for (const char* name : names)\n            if (name \&\& *name)\n                if (void* dll = dlopen(name, RTLD_LAZY))\n                    return dll;\n        return nullptr;\n    }\n' \
        -e 's|HMODULE dll|void* dll|' \
        -e 's|dll = GetModuleHandleW(L"vsscript.dll");|dll = open_vsscript();|' \
        -e '/^ *if (!dll)\r\?$/{N;/LoadLibraryW/d}' \
        -e 's|GetProcAddress(|dlsym(|' \
        -e 's|fopen_s(&fp, file_path, "rb");|fp = fopen(file_path, "rb");|' \
        -e 's|^static const AVS_Linkage\* AVS_linkage|const AVS_Linkage* AVS_linkage|' \
        -e '/int length = MultiByteToWideChar/,/return utf8_buff;/c\        std::vector<char> utf8_buff(ansi_string.begin(), ansi_string.end());\n        utf8_buff.push_back(0);\n        return utf8_buff;' \
        src/VapourSource.cpp
    avs_export src/VapourSource.cpp
    # the uninstall target of its Linux rules lacks its script
    touch cmake_uninstall.cmake.in
    CXXFLAGS="$CXXFLAGS -I$PREFIX/include/vapoursynth $(std_format_shim)" cmake_plugin
    plugin_install '*vapoursource*.so*' Plugins/AVS/VapourSource libVapourSource_x64.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/AVS/VapourSource
}
