version=R2-07e0589 # renovate: datasource=git-refs depName=https://github.com/vapoursynth/vs-miscfilters-obsolete.git branch=master digest=07e0589a381f7deb3bf533bb459a94482bccc5c7
packages="
MiscFilters vs
"
depends="vapoursynth"
sources="
git https://github.com/vapoursynth/vs-miscfilters-obsolete.git 07e0589a381f7deb3bf533bb459a94482bccc5c7
"

build() {
    unpack vs-miscfilters-obsolete-07e0589a381f.tar
    cd vs-miscfilters-obsolete-07e0589a381f
    meson_plugin
    plugin_install 'lib*.so*' Plugins/VS/MiscFilters libMiscFilters.so
    stage_license_files
}

bundle() {
    plugin_bundle Plugins/VS/MiscFilters
}
