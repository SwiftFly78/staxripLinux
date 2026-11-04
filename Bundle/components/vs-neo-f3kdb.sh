version=r11 # renovate: datasource=github-tags depName=HomeOfAviSynthPlusEvolution/neo_f3kdb versioning=regex:^[rR](?<major>\d+)(?:\.(?<minor>\d+))?$
packages="
f3kdb Neo dual
"
depends="avisynthplus vapoursynth"
sources="
git https://github.com/HomeOfAviSynthPlusEvolution/neo_f3kdb.git 939e6fabbe69303a3fbf0ee7d776df4b2381acca
"

build() {
    unpack neo_f3kdb-939e6fabbe69.tar
    cd neo_f3kdb-939e6fabbe69
    # the version comes from git describe, no TBB for the parallel algorithms
    git_version_tag r11
    cmake_plugin -DENABLE_PAR=OFF
    plugin_install '*neo-f3kdb*.so*' 'Plugins/Dual/f3kdb Neo' libneo-f3kdb.so
    stage_license_files
}

bundle() {
    plugin_bundle 'Plugins/Dual/f3kdb Neo'
}
