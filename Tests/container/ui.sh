#!/bin/bash
# The ui suite: StaxRip on X11 with Xvfb and openbox, on Wayland with sway, KWin, which runs in sway, and GNOME
# Shell, driven by the pointer of the display server like by a user. ui.ps1 runs the PowerShell code of the tests in
# StaxRip, to find widgets and their positions. Failures save renders of the windows to /work/out/ui-*.png.
SUITE=ui
. /t/common.sh
: > /work/results/ui.tsv
ulimit -c 0
export HOME=/tmp/home STAXRIP_SETTINGS=/tmp/home/settings NO_AT_BRIDGE=1 XDG_RUNTIME_DIR=/tmp/xdg
mkdir -p $HOME $XDG_RUNTIME_DIR
chmod 700 $XDG_RUNTIME_DIR
# for D-Bus and KWin
echo "ui:x:$(id -u):$(id -g)::$HOME:/bin/sh" >> /etc/passwd

# a session bus without services
cat > /tmp/bus.conf << EOF
<busconfig>
  <type>session</type>
  <listen>unix:path=$XDG_RUNTIME_DIR/bus</listen>
  <auth>EXTERNAL</auth>
  <policy context="default">
    <allow send_destination="*" eavesdrop="true"/>
    <allow eavesdrop="true"/>
    <allow own="*"/>
  </policy>
</busconfig>
EOF
dbus-daemon --config-file=/tmp/bus.conf --fork > /dev/null
export DBUS_SESSION_BUS_ADDRESS=unix:path=$XDG_RUNTIME_DIR/bus

Xvfb :99 -screen 0 1600x1000x24 -nolisten tcp > /tmp/xvfb.log 2>&1 &
export DISPLAY=:99
for i in $(seq 1 100); do
    xdotool getdisplaygeometry > /dev/null 2>&1 && break
    sleep 0.1
done

# confirms the changelog, the later starts don't show it
if ! first_start; then
    result fail "first start" "$(tail -3 /tmp/staxrip.out)"
    exit 1
fi

# the bitmap subtitles of the OCR test and the English of Tesseract, which isn't downloaded then
python3 /t/pgs.py /tmp/ocr-1.sup "First subtitle" "with two lines"
python3 /t/pgs.py /tmp/ocr-2.sup "Second subtitle"
mkdir -p $STAXRIP_SETTINGS/tessdata
cp /usr/share/tessdata/eng.traineddata $STAXRIP_SETTINGS/tessdata

# ask <code>: runs PowerShell code in StaxRip, prints its output and its error to stderr. The id is the time, a
# counter would be lost in subshells.
ask() {
    local i error id=${EPOCHREALTIME//[!0-9]/}
    printf '#%s\n%s\n' $id "$1" > /tmp/ui/cmd.tmp
    mv /tmp/ui/cmd.tmp /tmp/ui/cmd.ps1

    for i in $(seq 1 300); do
        if grep -q "^DONE $id$" /tmp/ui/log; then
            sed -n "s/^OUT $id //p" /tmp/ui/log
            error=$(sed -n "s/^ERROR $id //p" /tmp/ui/log)
            [ -z "$error" ] && return 0
            echo "$error" >&2
            return 1
        fi

        if ! kill -0 $app 2> /dev/null; then
            echo "StaxRip ended" >&2
            return 1
        fi

        sleep 0.1
    done

    echo "StaxRip didn't run $1" >&2
    return 1
}

# a PowerShell string
q() { printf "'%s'" "${1//\'/\'\'}"; }

# wait_for <code> [<seconds>]: waits until the PowerShell code returns True
wait_for() {
    local i
    for i in $(seq 1 $((${2:-5} * 5))); do
        [ "$(ask "$1" 2> /dev/null)" = True ] && return 0
        sleep 0.2
    done
    return 1
}

# enter <x> <y> <width> <height>: moves the pointer out of the menus, then into the rectangle from its left edge to its
# center like from a parent menu. GTK ignores a direct move from an item with an open submenu to another item, a way
# across the menu bar opens other menus, and on Wayland menus don't select the item a single move ends on.
enter() {
    local y=$(($2 + $4 / 2)) step
    move 1590 990

    for step in 4 $(($3 / 4)) $(($3 / 2)); do
        sleep 0.05
        move $(($1 + step)) $y
    done
}

# origin <title>: sets ox and oy to the position of the surface of a window on Wayland, which GTK doesn't know: the
# pointer is moved to the center of the window and GTK is asked where it is in the window
declare -A origins
origin() {
    local rect x y w h pointer= px py i
    if [ -z "${origins[$1]:-}" ]; then
        # the desktop gets the title of a new window a moment after GTK
        for i in $(seq 1 25); do
            rect=$(window_rect "$1")
            read -r x y w h <<< "$rect"
            [ "${h:-0}" -gt 0 ] 2> /dev/null && break
            sleep 0.2
        done

        if ! [ "${h:-0}" -gt 0 ] 2> /dev/null; then
            echo "no window $1" >&2
            return 1
        fi

        # a second and third attempt like a user moves the pointer again
        for i in 1 2 3; do
            enter $x $y $w $h
            sleep 0.2
            pointer=$(ask "Pointer $(q "$1")" 2> /dev/null) && break
        done

        if [ -z "$pointer" ]; then
            echo "the pointer isn't over $1" >&2
            return 1
        fi

        read -r px py <<< "$pointer"
        origins[$1]="$((x + w / 2 - px)) $((y + h / 2 - py))"
    fi

    read -r ox oy <<< "${origins[$1]}"
}

# menu_origin <width> <height>: sets ox and oy to the position of the surface of an open menu with the size from the
# desktop, if it knows them
menu_origin() { return 1; }

# point <rectangle of Rect>: moves the pointer to the center of a widget
point() {
    if ! [[ "$*" =~ ^-?[0-9]+(\ -?[0-9]+){9}( |$) ]]; then
        echo "no widget: $*" >&2
        return 1
    fi

    if [ "$9" -gt 0 ] && menu_origin "$9" "${10}"; then
        enter $((ox + $7)) $((oy + $8)) $3 $4
        return
    fi

    if [ $env = x11 ]; then
        ox=$5 oy=$6
    else
        origin "${*:11}" || return 1
    fi

    enter $((ox + $1)) $((oy + $2)) $3 $4
}

# the first problem of a test
problem() { [ -n "$problem" ] || problem=$1; }

# the menus a test opened
opened=0

# before_test: prepares the desktop for a test
before_test() { :; }

# before every test, the main window has no menu or dialog open
begin() {
    local main
    problem=
    origins=()
    before_test
    criticals=$(grep -c "Gtk-CRITICAL" /tmp/ui-$env.out)
    main=$(ask '$g.MainForm.ControlObject.Title') || problem "StaxRip doesn't run"
    [ $env = x11 ] || origin "$main" || problem "the main window wasn't found"
}

finish() {
    local critical
    critical=$(grep "Gtk-CRITICAL" /tmp/ui-$env.out | tail -n +$((criticals + 1)) | head -1)
    [ -n "$critical" ] && problem "$critical"

    if [ -z "$problem" ]; then
        result pass "$env: $1" "${2:-}"
        return
    fi

    result fail "$env: $1" "$problem"
    ask "Snap 'ui-$env-${1// /-}'" > /dev/null 2>&1
    [ $env = sway ] && grim "/work/out/ui-$env-${1// /-}-screen.png"
    ask '(MenuBar).Deactivate(); if ($g.ProcForm.Visible) { [StaxRip.ProcController]::Abort() }' > /dev/null 2>&1
    sleep 1
    ask CloseDialogs > /dev/null 2>&1
    sleep 0.5
}

# is_open <path>: waits until a menu is open and keeps its position, on Wayland the final position of a menu that
# had to be moved to fit on the screen comes later
is_open() {
    local last= now i
    wait_for "IsOpen $(q "$1")" 3 || return 1

    for i in $(seq 1 20); do
        now=$(ask "Rect (Item $(q "$1")).Submenu.Toplevel" 2> /dev/null)
        [ -n "$now" ] && [ "$now" = "$last" ] && return 0
        last=$now
        sleep 0.1
    done
}

# hover_item <path>: hovers an item with a submenu until the submenu is open
hover_item() {
    local rect
    rect=$(ask "Rect (Item $(q "$1"))" 2> /dev/null) && point $rect && is_open "$1"
}

# open_menu <path>: opens a menu of the menu bar by clicking, a submenu by hovering, which is tried again like a user
# moves the pointer again
open_menu() {
    local rect

    if [[ "$1" == *" > "* ]]; then
        hover_item "$1" || hover_item "$1" || { problem "the menu $1 didn't open"; return 1; }
    elif ! rect=$(ask "Rect (Item $(q "$1"))" 2>&1) || ! point $rect || ! click || ! is_open "$1"; then
        problem "the menu $1 didn't open: $rect"
        return 1
    fi

    opened=$((opened + 1))
}

# close_menu <menu>: clicks a menu of the menu bar again, which closes it with its submenus and ends the grab of GTK,
# replacing an open submenu once left a grab that blocked all input
close_menu() {
    local rect
    rect=$(ask "Rect (Item $(q "$1"))" 2>&1) && point $rect && click
    wait_for '(Popups) -eq 0 -and -not (HasGrab)'
}

# hover <path>: hovers every item of an open menu that has a submenu, and the items of the submenu
hover() {
    local names name
    mapfile -t names < <(ask "Submenus $(q "$1")")

    for name in "${names[@]}"; do
        open_menu "$1 > $name" && hover "$1 > $name"
    done
}

click_item() {
    local rect
    if rect=$(ask "Rect (Item $(q "$1"))" 2>&1) && point $rect; then
        click
    else
        problem "the menu item $1 wasn't found: $rect"
    fi
}

click_button() {
    local rect
    if rect=$(ask "Rect (Button $(q "$1"))" 2>&1) && point $rect; then
        click
    else
        problem "the button $1 wasn't found: $rect"
    fi
}

# opens every menu of the menu bar and every submenu, also the ones StaxRip fills when they open
menus() {
    local menus menu
    begin
    opened=0
    mapfile -t menus < <(ask 'Items (MenuBar) | Where-Object { $_.Submenu } | ForEach-Object { Label $_ }')
    [ ${#menus[@]} -gt 3 ] || problem "the menu bar has ${#menus[@]} menus"

    for menu in "${menus[@]}"; do
        open_menu "$menu" && hover "$menu"
        close_menu "$menu" || problem "clicking $menu again didn't close its menus"
    done

    [ "$(ask '@(Dialogs).Count')" = 0 ] || problem "a window opened: $(ask Dialogs)"
    finish menus "$opened menus"
}

# runs the script Hello World from Tools > Scripts and closes its message
script() {
    begin
    open_menu Tools && open_menu "Tools > Scripts" && click_item "Tools > Scripts > Hello World"
    wait_for "HasText 'Hello World'" || problem "the script didn't show its message"
    click_button OK
    wait_for "@(Dialogs).Count -eq 0" || problem "the message didn't close"
    finish "script from the Scripts menu"
}

# Copy Message of an error copies its title and text, another client reads them from the clipboard. The line breaks
# are the ones of Windows.
clipboard() {
    local text expected=$'UI test error\r\n\r\nCopy Message copies the title and this text.'
    begin
    ask "Later { [StaxRip.MainModule]::MsgError('UI test error', 'Copy Message copies the title and this text.') }"
    wait_for "HasText 'UI test error'" || problem "no error dialog"
    click_button "Copy Message"
    sleep 0.5
    text=$(clipboard_text 2>&1)
    [ "$text" = "$expected" ] || problem "the clipboard has: $text"
    click_button OK
    wait_for "@(Dialogs).Count -eq 0" || problem "the error didn't close"
    finish clipboard
}

# a process started in a dialog, like OCR in the options of the container, shows the processing window above the
# dialog, whose Abort button works, and the dialog stays. Hiding the main window unmapped the dialog on GNOME, its
# grab then blocked all input.
processing() {
    begin
    ask 'Later { $g.MainForm.bnMuxerOptions_Click() }' > /dev/null
    wait_for "@(Dialogs).Count -eq 1" || problem "the options of the container didn't open"
    ask "Later { \$p = [StaxRip.Proc]::new(\$true); \$p.Header = 'UI test'; \$p.File = 'sleep'; \$p.Arguments = '60';
        try { \$p.Start() } finally { \$p.Dispose() } }" > /dev/null
    wait_for '$g.ProcForm.Visible' || problem "the processing window didn't open"
    click_button "Abort (ESC)"
    wait_for "HasText 'Abort processing?'" || problem "Abort didn't ask whether to abort"
    click_button OK
    wait_for '-not $g.ProcForm.Visible' || problem "Abort didn't end the processing"
    click_button Cancel
    wait_for "@(Dialogs).Count -eq 0" || problem "the options of the container didn't close"
    open_menu File && close_menu File
    finish "processing in a dialog"
}

# OCR to SRT of two selected bitmap subtitles in the options of the container, with the dialog of their languages
ocr() {
    local muxer='[Eto.Forms.Application]::Instance.Windows | Where-Object { $_ -is [StaxRip.MuxerForm] }'
    begin
    rm -f /tmp/ocr-*.srt
    ask 'Later { $g.MainForm.bnMuxerOptions_Click() }' > /dev/null
    wait_for "@(Dialogs).Count -eq 1" || problem "the options of the container didn't open"
    ask "\$f = $muxer; \$f.tcMain.SelectedIndex = 1
        foreach (\$i in 1, 2) { \$f.AddSubtitles([StaxRip.Subtitle]::Create(\"/tmp/ocr-\$i.sup\")) }
        \$f.dgvSubtitles.SelectAll(); \$f.UpdateControls()" > /dev/null
    click_button "OCR to SRT..."
    wait_for "@(@(Dialogs) -match '^OCR to SRT').Count -eq 1" || problem "the dialog of the OCR languages didn't open"
    click_button Convert
    wait_for "@(($muxer).dgvSubtitles.DataStore | Where-Object { \$_.TypeName -eq 'SRT' }).Count -eq 2" 30 ||
        problem "the subtitles weren't converted"
    wait_for '-not $g.ProcForm.Visible' || problem "the processing window didn't close"
    grep -q "First subtitle" /tmp/ocr-1.srt && grep -q "with two lines" /tmp/ocr-1.srt &&
        grep -q "Second subtitle" /tmp/ocr-2.srt || problem "wrong text: $(cat /tmp/ocr-*.srt 2>&1 | tr '\n' ' ')"
    click_button Cancel
    wait_for "@(Dialogs).Count -eq 0" || problem "the options of the container didn't close"
    finish "OCR of two subtitles"
}

tests=(menus "script from the Scripts menu" clipboard "processing in a dialog" "OCR of two subtitles")

# all <result> <detail>: the result of every test of the environment
all() {
    local test
    for test in "${tests[@]}"; do
        result "$1" "$env: $test" "$2"
    done
}

# run <variable>=<value>...: runs the tests in StaxRip with the variables, which starts with the command loop of
# ui.ps1
run() {
    rm -rf /tmp/ui
    mkdir -p /tmp/ui
    : > /tmp/ui/log
    timeout -k 10 900 env -u DISPLAY -u WAYLAND_DISPLAY "$@" /tmp/app/AppRun \
        "-ExecutePowerShellFile:/t/ui.ps1,/tmp/ui" > /tmp/ui-$env.out 2>&1 &
    app=$!

    for i in $(seq 1 600); do
        grep -q "^LOOP" /tmp/ui/log && break
        kill -0 $app 2> /dev/null || break
        sleep 0.1
    done

    if grep -q "^LOOP" /tmp/ui/log; then
        menus
        script
        clipboard
        processing
        ocr
        printf '#0\n[Environment]::Exit(0)\n' > /tmp/ui/cmd.tmp
        mv /tmp/ui/cmd.tmp /tmp/ui/cmd.ps1
    else
        all fail "StaxRip didn't start: $(grep -v '^\s*$' /tmp/ui-$env.out | tail -3)"
    fi

    for i in $(seq 1 100); do
        kill -0 $app 2> /dev/null || break
        sleep 0.1
    done

    kill $app 2> /dev/null
    wait $app 2> /dev/null
    cp /tmp/ui-$env.out /work/out/ui-$env.out
}

# wait_socket <path>: waits for the socket of the compositor
wait_socket() {
    for i in $(seq 1 100); do
        [ -S "$1" ] && return 0
        kill -0 $compositor 2> /dev/null || return 1
        sleep 0.1
    done
    return 1
}

env=x11
openbox --sm-disable > /tmp/openbox.log 2>&1 &
compositor=$!
move() { xdotool mousemove "$1" "$2"; }
click() { xdotool click 1; }
clipboard_text() { timeout 5 xclip -o -selection clipboard; }
run DISPLAY=:99 GDK_BACKEND=x11
kill $compositor

# the input client of sway and KWin
(cd /tmp &&
    for xml in /usr/share/plasma-wayland-protocols/fake-input.xml \
        /usr/share/wlr-protocols/unstable/wlr-virtual-pointer-unstable-v1.xml; do
        name=$(basename $xml .xml)
        wayland-scanner client-header $xml $name-client-protocol.h || exit 1
        wayland-scanner private-code $xml $name-protocol.c || exit 1
    done &&
    gcc -O2 -I/tmp -o /tmp/input /t/input.c fake-input-protocol.c wlr-virtual-pointer-unstable-v1-protocol.c \
        -lwayland-client -lm) > /tmp/input.log 2>&1

# a headless sway has no input devices, a virtual pointer and keyboard are added to its seat, the window of StaxRip
# isn't active without a keyboard. Menus of GTK ignore the keys of virtual keyboards, so the tests only use the
# pointer, which swaymsg moves.
env=sway
printf '%s\n' 'output HEADLESS-1 resolution 1600x1000 position 0 0' 'xwayland disable' 'default_border none' \
    'default_floating_border none' 'focus_follows_mouse no' > /tmp/sway.conf
WLR_BACKENDS=headless WLR_RENDERER=pixman WLR_LIBINPUT_NO_DEVICES=1 sway -c /tmp/sway.conf > /tmp/sway.log 2>&1 &
sway=$!
compositor=$sway
for i in $(seq 1 100); do
    export SWAYSOCK=$(ls $XDG_RUNTIME_DIR/sway-ipc.*.sock 2> /dev/null | head -1)
    export WAYLAND_DISPLAY=$(cd $XDG_RUNTIME_DIR && ls wayland-? 2> /dev/null | head -1)
    [ -n "$SWAYSOCK" ] && [ -n "$WAYLAND_DISPLAY" ] && break
    sleep 0.1
done
host=$WAYLAND_DISPLAY
move() { swaymsg -q seat seat0 cursor set "$1" "$2"; }
click() { swaymsg -q seat seat0 cursor press button1 && sleep 0.05 && swaymsg -q seat seat0 cursor release button1; }
clipboard_text() { timeout 5 wl-paste -n; }
window_rect() {
    swaymsg -t get_tree | jq -r --arg title "$1" '[.. | objects | select(.pid?)
        | select((.name | gsub("^\\s+|\\s+$"; "")) == $title)]
        | (map(select(.focused)) + .)[0].rect // empty | "\(.x) \(.y) \(.width) \(.height)"'
}
if [ ! -x /tmp/input ]; then
    all fail "the input client wasn't built: $(tail -3 /tmp/input.log)"
elif wait_socket "$XDG_RUNTIME_DIR/$host"; then
    /tmp/input hold &
    holds="$!"
    wtype -s 86400000 &
    holds+=" $!"
    sleep 0.5
    run WAYLAND_DISPLAY=$host GDK_BACKEND=wayland
    kill $holds
else
    all fail "sway didn't start: $(tail -3 /tmp/sway.log)"
fi

# KWin runs in a window of sway with software compositing, so it needs no GPU. Its seat has no pointer without the
# device of a fake input client.
env=kwin
KWIN_COMPOSE=Q KWIN_WAYLAND_NO_PERMISSION_CHECKS=1 QT_FORCE_STDERR_LOGGING=1 \
    QT_LOGGING_RULES=kwin_scripting.debug=true env -u DISPLAY WAYLAND_DISPLAY=$host kwin_wayland \
    --wayland-display $host --width 1600 --height 1000 --socket wl-kwin --no-lockscreen --no-global-shortcuts \
    --no-kactivities > /tmp/kwin.log 2>&1 &
compositor=$!
export WAYLAND_DISPLAY=wl-kwin
move() { /tmp/input move "$1" "$2"; }
click() { /tmp/input click; }
clipboard_text() { timeout 5 wl-paste -n; }
# kwin_script <JavaScript>: runs a KWin script, prints what it passes to out()
script_id=0
kwin_script() {
    local name id
    script_id=$((script_id + 1))
    name=ui$script_id
    printf 'function out(text) { print("%s " + text); }\n%s\nout("END");\n' $name "$1" > /tmp/$name.js
    gdbus call --session --dest org.kde.KWin --object-path /Scripting --method org.kde.kwin.Scripting.loadScript \
        /tmp/$name.js $name > /tmp/kwin-script.out || return 1
    id=$(grep -o '[0-9]\+' /tmp/kwin-script.out)
    gdbus call --session --dest org.kde.KWin --object-path /Scripting/Script$id --method org.kde.kwin.Script.run \
        > /dev/null

    for i in $(seq 1 50); do
        grep -q "js: $name END" /tmp/kwin.log && break
        sleep 0.1
    done

    gdbus call --session --dest org.kde.KWin --object-path /Scripting --method org.kde.kwin.Scripting.unloadScript \
        $name > /dev/null
    sed -n "s/^js: $name //p" /tmp/kwin.log | grep -v '^END$'
}
window_rect() {
    kwin_script "for (const w of workspace.stackingOrder) if (w.caption.trim() == $(jq -n --arg title "$1" '$title')) {
        const r = w.clientGeometry; out([r.x, r.y, r.width, r.height].map(Math.round).join(' ')); }" | tail -1
}
if [ ! -x /tmp/input ]; then
    all fail "the input client wasn't built: $(tail -3 /tmp/input.log)"
elif wait_socket "$XDG_RUNTIME_DIR/wl-kwin"; then
    /tmp/input hold &
    holds=$!
    sleep 0.5
    run WAYLAND_DISPLAY=wl-kwin GDK_BACKEND=wayland
    kill $holds
else
    all fail "KWin didn't start: $(tail -3 /tmp/kwin.log)"
fi
kill $compositor $sway
wait $compositor $sway 2> /dev/null
cp /tmp/kwin.log /work/out/ui-kwin.log

# GNOME Shell runs headless, its Eval moves virtual devices, finds windows and reads the clipboard. A session sets
# XDG_CURRENT_DESKTOP, which enables the GNOME defaults of mutter like attached modal dialogs, and has a system bus.
# The pointer of a new device is in the hot corner, which opens the overview, the settings without dconf disable it.
env=gnome
mkdir -p $HOME/.config/glib-2.0/settings
printf '[org/gnome/desktop/interface]\nenable-hot-corners=false\n' > $HOME/.config/glib-2.0/settings/keyfile
sed "s|<type>session</type>|<type>system</type>|; s|$XDG_RUNTIME_DIR/bus|$XDG_RUNTIME_DIR/system-bus|" /tmp/bus.conf \
    > /tmp/system-bus.conf
dbus-daemon --config-file=/tmp/system-bus.conf --fork > /dev/null
XDG_CURRENT_DESKTOP=GNOME DBUS_SYSTEM_BUS_ADDRESS=unix:path=$XDG_RUNTIME_DIR/system-bus GSETTINGS_BACKEND=keyfile \
    env -u DISPLAY -u WAYLAND_DISPLAY gnome-shell --headless --wayland --no-x11 --unsafe-mode \
    --virtual-monitor 1600x1000 --wayland-display wl-gnome > /tmp/gnome-shell.log 2>&1 &
compositor=$!
export WAYLAND_DISPLAY=wl-gnome
# shell <JavaScript>: runs JavaScript in GNOME Shell, prints its value
shell() {
    gdbus call --session --dest org.gnome.Shell --object-path /org/gnome/Shell --method org.gnome.Shell.Eval "$1" \
        2> /dev/null | sed -E "s/^\(true, '\"?//; s/\"?'\)$//"
}
move() { shell "global.uiPointer.notify_absolute_motion(GLib.get_monotonic_time(), $1, $2)" > /dev/null; }
click() {
    shell 'global.uiPointer.notify_button(GLib.get_monotonic_time(), 1, 1)' > /dev/null
    sleep 0.05
    shell 'global.uiPointer.notify_button(GLib.get_monotonic_time(), 1, 0)' > /dev/null
}
# the text comes as base64, Eval returns strings escaped
clipboard_text() {
    local i text
    shell "global.uiClipboard = null; const St = imports.gi.St;
        St.Clipboard.get_default().get_text(St.ClipboardType.CLIPBOARD, (c, text) => {
            global.uiClipboard = text ?? ''; })" > /dev/null

    for i in $(seq 1 25); do
        text=$(shell "global.uiClipboard === null ? 'none' :
            GLib.base64_encode(new TextEncoder().encode(global.uiClipboard))")
        [ "$text" = none ] || { printf '%s' "$text" | base64 -d; return; }
        sleep 0.2
    done
}
window_rect() {
    shell "const title = $(jq -n --arg title "$1" '$title');
        const w = global.display.list_all_windows().find(w => w.get_title()?.trim() == title);
        if (w) { const r = w.get_frame_rect(); [r.x, r.y, r.width, r.height].join(' '); } else { ''; }"
}
menu_origin() {
    local origin
    origin=$(shell "const w = global.get_window_actors().map(a => a.meta_window).reverse().find(w =>
        [9, 10].includes(w.get_window_type()) && w.get_buffer_rect().width == $1 && w.get_buffer_rect().height == $2);
        w ? w.get_buffer_rect().x + ' ' + w.get_buffer_rect().y : ''")
    [ -n "$origin" ] && read -r ox oy <<< "$origin"
}
# the overview opens again shortly after the start of GNOME Shell and when there are no windows, its window previews
# then get the pointer. Hidden during its start animation, the previews stay, it's shown and hidden again then.
before_test() {
    local i hidden=0
    for i in $(seq 1 50); do
        if [ "$(shell 'const o = Main.overview, previews = Main.layoutManager.overviewGroup;
            if (!o.animationInProgress) { if (o.visible) { o.hide(); } else if (previews.visible) { o.show(); } }
            o.visible || previews.visible || o.animationInProgress || Main.modalCount > 0')" = false ]; then
            hidden=$((hidden + 1))
            [ $hidden = 3 ] && return
        else
            hidden=0
        fi

        sleep 0.3
    done
}
# hiding the overview before the end of the start animation keeps the windows from getting input
for i in $(seq 1 300); do
    [ "$(shell 'Main.layoutManager._startingUp')" = false ] && break
    kill -0 $compositor 2> /dev/null || break
    sleep 0.1
done
if [ "$(shell 'Main.layoutManager._startingUp')" = false ]; then
    shell "const Clutter = imports.gi.Clutter; const seat = global.stage.context.get_backend().get_default_seat();
        global.uiPointer = seat.create_virtual_device(Clutter.InputDeviceType.POINTER_DEVICE);
        global.uiKeyboard = seat.create_virtual_device(Clutter.InputDeviceType.KEYBOARD_DEVICE);
        global.uiPointer.notify_absolute_motion(GLib.get_monotonic_time(), 800, 500)" > /dev/null
    run WAYLAND_DISPLAY=wl-gnome GDK_BACKEND=wayland XDG_CURRENT_DESKTOP=GNOME
else
    all fail "GNOME Shell didn't start: $(grep -v '^\s*$' /tmp/gnome-shell.log | tail -3)"
fi
kill $compositor
wait $compositor 2> /dev/null
