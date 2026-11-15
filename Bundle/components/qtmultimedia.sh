# for the GUI of MKVToolNix, which links it but only uses it for sounds after the jobs, so it's built without media
# backend and audio output, the DMA-BUF import of video frames needs the EGL that Qt Base is built without
version=6.12.0 # renovate: datasource=github-tags depName=qt/qtmultimedia extractVersion=^v(?<version>.+)$
depends="qtbase qtshadertools"
sources="
https://download.qt.io/official_releases/qt/6.12/6.12.0/submodules/qtmultimedia-everywhere-src-6.12.0.tar.xz 3143f53b64257ba2a0685c940f5dce476c75871ff86b516de45cfd54ea93a62d
"

build() {
    unpack qtmultimedia-everywhere-src-6.12.0.tar.xz
    cd qtmultimedia-everywhere-src-6.12.0
    cmake_build -DQT_BUILD_EXAMPLES=OFF -DQT_BUILD_TESTS=OFF -DFEATURE_ffmpeg=OFF -DFEATURE_gstreamer=OFF \
        -DFEATURE_spatialaudio=OFF -DFEATURE_pulseaudio=OFF -DFEATURE_linux_dmabuf=OFF -DFEATURE_vaapi=OFF
    stage_license LICENSES/LGPL-3.0-only.txt LICENSES/GPL-2.0-only.txt LICENSES/GPL-3.0-only.txt
}
