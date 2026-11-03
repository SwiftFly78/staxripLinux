# Qt Core, MKVToolNix needs it for the command line tools too
version=6.11.3 # renovate: datasource=github-tags depName=qt/qtbase extractVersion=^v(?<version>.+)$
sources="
https://download.qt.io/official_releases/qt/6.11/6.11.3/submodules/qtbase-everywhere-src-6.11.3.tar.xz 46de0710b594945a5d446ae44354daa386141c6903c53781b2ca547bc62f8f8f
"

build() {
    unpack qtbase-everywhere-src-6.11.3.tar.xz
    cd qtbase-everywhere-src-6.11.3
    cmake_build -DBUILD_SHARED_LIBS=ON -DQT_BUILD_EXAMPLES=OFF -DQT_BUILD_TESTS=OFF \
        -DFEATURE_gui=OFF -DFEATURE_widgets=OFF -DFEATURE_dbus=OFF -DFEATURE_network=OFF \
        -DFEATURE_sql=OFF -DFEATURE_testlib=OFF -DFEATURE_xml=OFF -DFEATURE_printsupport=OFF \
        -DFEATURE_glib=OFF -DFEATURE_icu=OFF -DFEATURE_zstd=OFF -DINPUT_openssl=no \
        -DFEATURE_system_doubleconversion=OFF -DFEATURE_system_pcre2=OFF
    stage_license LICENSES/LGPL-3.0-only.txt LICENSES/GPL-2.0-only.txt LICENSES/GPL-3.0-only.txt
}
