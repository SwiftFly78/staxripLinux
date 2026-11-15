# for the GUI of MKVToolNix, its icons are SVG files
version=6.12.0 # renovate: datasource=github-tags depName=qt/qtsvg extractVersion=^v(?<version>.+)$
depends="qtbase"
sources="
https://download.qt.io/official_releases/qt/6.12/6.12.0/submodules/qtsvg-everywhere-src-6.12.0.tar.xz e4ab39534ec97987b1b9b60ef7f4d3253d5912a69f8c713a01753174a4029331
"

build() {
    unpack qtsvg-everywhere-src-6.12.0.tar.xz
    cd qtsvg-everywhere-src-6.12.0
    cmake_build -DQT_BUILD_EXAMPLES=OFF -DQT_BUILD_TESTS=OFF
    stage_license LICENSES/LGPL-3.0-only.txt LICENSES/GPL-2.0-only.txt LICENSES/GPL-3.0-only.txt
}

bundle() {
    qt_plugin_install iconengines/libqsvgicon.so imageformats/libqsvg.so
}
