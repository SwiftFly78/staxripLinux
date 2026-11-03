# A desktop-like Ubuntu 22.04, the oldest system the full AppImage supports, without any of the bundled tools, so
# missing libraries show up. xdotool confirms the changelog dialog of the first start, xwd takes screenshots.
# GPU=1 adds Mesa's software Vulkan driver lavapipe and the CPU OpenCL driver PoCL, for the plugins that need a GPU.
FROM ubuntu:22.04@sha256:076e7bf82319619f28dd0aa9c02ab34b055ad37100395b4f49a2ffba04fb5e11
ARG GPU=0
ENV DEBIAN_FRONTEND=noninteractive LANG=C.UTF-8
RUN apt-get update \
    && apt-get install -y libgtk-3-0 libicu70 xvfb xauth fonts-dejavu-core xdotool x11-apps \
    && if [ "$GPU" = 1 ]; then apt-get install -y mesa-vulkan-drivers pocl-opencl-icd; fi \
    && rm -rf /var/lib/apt/lists/*
