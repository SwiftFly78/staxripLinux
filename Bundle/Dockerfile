# Build environment of the bundled tools. Ubuntu 22.04 keeps the glibc requirement at 2.35,
# so the tools run on the distributions released since 2022.
FROM ubuntu:22.04@sha256:076e7bf82319619f28dd0aa9c02ab34b055ad37100395b4f49a2ffba04fb5e11

ENV DEBIAN_FRONTEND=noninteractive LANG=C.UTF-8

RUN apt-get update && apt-get install -y --no-install-recommends \
        autoconf automake autopoint bzip2 ca-certificates curl file g++-12 gcc-12 gettext git libtool make \
        nasm patchelf pkg-config python3 python3-pip rake ruby unzip xz-utils yasm zstd \
        glslang-dev libboost-chrono-dev libboost-dev libboost-filesystem-dev libboost-system-dev \
        libboost-thread-dev libbz2-dev libdevil-dev libdrm-dev libfftw3-dev libflac-dev libfontconfig-dev \
        libfreetype-dev libgmp-dev liblzma-dev libogg-dev libpng-dev libva-dev libvorbis-dev libvulkan-dev \
        libxxhash-dev ocl-icd-opencl-dev opencl-headers zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

COPY requirements-build.txt /tmp/
RUN python3 -m pip install --no-cache-dir --require-hashes -r /tmp/requirements-build.txt

# GCC 12 for the build systems that call gcc and cc, its LTO plugin for ar and nm, without it static libraries of
# builds with LTO have no symbol index, the prefix of the builds is writable for the user of the host the build
# runs as
RUN for i in gcc cc; do ln -sf gcc-12 /usr/bin/$i; done \
    && for i in g++ c++; do ln -sf g++-12 /usr/bin/$i; done \
    && mkdir -p /usr/lib/bfd-plugins && ln -s ../gcc/x86_64-linux-gnu/12/liblto_plugin.so /usr/lib/bfd-plugins/ \
    && mkdir -p /opt/sr && chmod 1777 /opt/sr

ENV CC=gcc-12 CXX=g++-12
