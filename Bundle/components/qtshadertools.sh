# Qt Multimedia needs its shader compiler to build
version=6.12.0 # renovate: datasource=github-tags depName=qt/qtshadertools extractVersion=^v(?<version>.+)$
depends="qtbase"
sources="
https://download.qt.io/official_releases/qt/6.12/6.12.0/submodules/qtshadertools-everywhere-src-6.12.0.tar.xz c7d84f436e1aaef39fdcadebf2bd71bdf24dc69497e13f2e0198873cbbfea2ab
"

build() {
    unpack qtshadertools-everywhere-src-6.12.0.tar.xz
    cd qtshadertools-everywhere-src-6.12.0
    cmake_build -DQT_BUILD_EXAMPLES=OFF -DQT_BUILD_TESTS=OFF
    stage_license LICENSES/LGPL-3.0-only.txt LICENSES/GPL-2.0-only.txt LICENSES/GPL-3.0-only.txt
}
