# the FFTW package of StaxRip, a link to the libfftw3f.so.3 of the Ubuntu package that collect.py copies for the
# plugins that need it
version=3.3.8
packages=FFTW
sources=""

build() {
    :
}

bundle() {
    mkdir -p $O/Apps/Support/FFTW
    ln -s ../../lib/libfftw3f.so.3 $O/Apps/Support/FFTW/libfftw3f.so.3
}
