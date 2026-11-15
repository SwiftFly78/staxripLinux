# the command line editing of the PowerShell terminal (StaxRipLinux --pwsh): history, completion and syntax colors
version=2.4.5 # renovate: datasource=github-releases depName=PowerShell/PSReadLine extractVersion=^v(?<version>.+)$
packages=PSReadLine
sources="
https://www.powershellgallery.com/api/v2/package/PSReadLine/2.4.5 cb9390e9733208456c234a7971d1ec4a917886c239502aab68f4b71aa4bba235 PSReadLine-2.4.5.nupkg
https://github.com/PowerShell/PSReadLine/archive/refs/tags/v2.4.5.tar.gz f2160930a98fcf5200ed50b37b1653e0f685036d5d4bcafa88da6912e5dff9cd PSReadLine-2.4.5.tar.gz
"
binary_sources="PSReadLine-2.4.5.nupkg"

build() {
    # the module is the package without the files of NuGet
    mkdir PSReadLine
    unzip -q $C/downloads/PSReadLine-2.4.5.nupkg -d PSReadLine
    rm -r PSReadLine/_rels PSReadLine/package "PSReadLine/[Content_Types].xml" PSReadLine/*.nuspec PSReadLine/.signature.p7s
    mkdir -p $DESTDIR$PREFIX/share/powershell
    cp -r PSReadLine $DESTDIR$PREFIX/share/powershell/
    stage_license PSReadLine/License.txt
}

bundle() {
    mkdir -p $O/Apps/Support/PowerShell
    cp -r $PREFIX/share/powershell/PSReadLine $O/Apps/Support/PowerShell/
}
