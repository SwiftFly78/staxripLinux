# Qt Core, MKVToolNix needs it for the command line tools too
version=6.12.0 # renovate: datasource=github-tags depName=qt/qtbase extractVersion=^v(?<version>.+)$
sources="
https://download.qt.io/official_releases/qt/6.12/6.12.0/submodules/qtbase-everywhere-src-6.12.0.tar.xz a951bd163c7b80fc6b8c88d7668fb56abf91c152373e13c10666763238131307
"

build() {
    unpack qtbase-everywhere-src-6.12.0.tar.xz
    cd qtbase-everywhere-src-6.12.0
    cmake_build -DBUILD_SHARED_LIBS=ON -DQT_BUILD_EXAMPLES=OFF -DQT_BUILD_TESTS=OFF \
        -DFEATURE_gui=OFF -DFEATURE_widgets=OFF -DFEATURE_dbus=OFF -DFEATURE_network=OFF \
        -DFEATURE_sql=OFF -DFEATURE_testlib=OFF -DFEATURE_xml=OFF -DFEATURE_printsupport=OFF \
        -DFEATURE_glib=OFF -DFEATURE_icu=OFF -DFEATURE_zstd=OFF -DINPUT_openssl=no \
        -DFEATURE_system_doubleconversion=OFF -DFEATURE_system_pcre2=OFF
    stage_license LICENSES/LGPL-3.0-only.txt LICENSES/GPL-2.0-only.txt LICENSES/GPL-3.0-only.txt
}
