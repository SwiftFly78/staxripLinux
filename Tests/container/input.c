// Input events for the compositors of the ui suite: input <command>...
// KWin, through its fake input protocol: move <x> <y>, click, key <evdev code>
// hold: keeps a pointer device until the process ends, a virtual pointer of wlroots or a fake input device of KWin.
// The seats of a headless sway and KWin have no pointer without it, the devices of the commands end with them.
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <wayland-client.h>
#include "fake-input-client-protocol.h"
#include "wlr-virtual-pointer-unstable-v1-client-protocol.h"

static struct org_kde_kwin_fake_input *fake_input;
static struct zwlr_virtual_pointer_manager_v1 *pointer_manager;
static struct wl_seat *seat;

static void add(void *data, struct wl_registry *registry, uint32_t name, const char *interface, uint32_t version)
{
    if (strcmp(interface, org_kde_kwin_fake_input_interface.name) == 0)
        fake_input = wl_registry_bind(registry, name, &org_kde_kwin_fake_input_interface, version < 4 ? version : 4);
    else if (strcmp(interface, zwlr_virtual_pointer_manager_v1_interface.name) == 0)
        pointer_manager = wl_registry_bind(registry, name, &zwlr_virtual_pointer_manager_v1_interface, 1);
    else if (strcmp(interface, wl_seat_interface.name) == 0 && !seat)
        seat = wl_registry_bind(registry, name, &wl_seat_interface, 1);
}

static void remove_global(void *data, struct wl_registry *registry, uint32_t name) {}

static const struct wl_registry_listener listener = { add, remove_global };

int main(int argc, char **argv)
{
    struct wl_display *display = wl_display_connect(NULL);
    if (!display) {
        fprintf(stderr, "no Wayland display\n");
        return 1;
    }

    wl_registry_add_listener(wl_display_get_registry(display), &listener, NULL);
    wl_display_roundtrip(display);

    if (argc == 2 && !strcmp(argv[1], "hold")) {
        if (pointer_manager && seat) {
            zwlr_virtual_pointer_manager_v1_create_virtual_pointer(pointer_manager, seat);
        } else if (fake_input) {
            org_kde_kwin_fake_input_authenticate(fake_input, "StaxRipLinux tests", "ui suite");
        } else {
            fprintf(stderr, "no virtual pointer or fake input\n");
            return 1;
        }

        while (wl_display_dispatch(display) != -1) {}
        return 0;
    }

    if (!fake_input) {
        fprintf(stderr, "no fake input\n");
        return 1;
    }

    org_kde_kwin_fake_input_authenticate(fake_input, "StaxRipLinux tests", "ui suite");

    for (int i = 1; i < argc; i++) {
        if (!strcmp(argv[i], "move") && i + 2 < argc) {
            double x = atof(argv[i + 1]), y = atof(argv[i + 2]);
            org_kde_kwin_fake_input_pointer_motion_absolute(fake_input, wl_fixed_from_double(x),
                wl_fixed_from_double(y));
            i += 2;
        } else if (!strcmp(argv[i], "click")) {
            org_kde_kwin_fake_input_button(fake_input, 0x110, 1);
            wl_display_roundtrip(display);
            usleep(50000);
            org_kde_kwin_fake_input_button(fake_input, 0x110, 0);
        } else if (!strcmp(argv[i], "key") && i + 1 < argc) {
            int code = atoi(argv[++i]);
            org_kde_kwin_fake_input_keyboard_key(fake_input, code, 1);
            wl_display_roundtrip(display);
            usleep(50000);
            org_kde_kwin_fake_input_keyboard_key(fake_input, code, 0);
        } else {
            fprintf(stderr, "unknown command %s\n", argv[i]);
            return 2;
        }

        wl_display_roundtrip(display);
    }

    wl_display_disconnect(display);
    return 0;
}
