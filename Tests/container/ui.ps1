# The command loop of the ui suite: runs the PowerShell code ui.sh writes to <dir>/cmd.ps1, whose first line is
# "#<id>", and appends "OUT <id> <line>" for each line of its output, "ERROR <id> <message>" and "DONE <id>" to
# <dir>/log. A timer of C# runs the code in a new runspace with the functions below, like StaxRip runs scripts, and this
# script returns: a dialog shown by a script gets no input while the pipeline of another script waits below it. The
# timer also runs while a modal dialog is open, which code has to open with Later.
$dir = $args[0]

$functions = @'
$g = [StaxRip.ShortcutModule]::g

# runs code once from its own timer, for code that doesn't return before the dialog it opens is closed
function Later([scriptblock]$code) { [UiTestLoop]::Later($code.ToString()) }

# GTK containers enumerate their children, the comma keeps PowerShell from returning them instead
function Find($widget, [scriptblock]$test) {
    if (& $test $widget) { return ,$widget }

    if ($widget -is [Gtk.Container]) {
        foreach ($child in $widget.Children) {
            $found = Find $child $test
            if ($null -ne $found) { return ,$found }
        }
    }
}

function Label($widget) {
    $label = Find $widget { param($w) $w -is [Gtk.Label] }
    if ($label) { $label.Text }
}

function Windows { [Gtk.Window]::ListToplevels() | Where-Object { $_.IsMapped } }

function Window([string]$title) {
    $window = Windows | Where-Object { $_.Title -eq $title } | Select-Object -First 1
    if (-not $window) { throw "no window $title" }
    ,$window
}

# the windows besides the main window, menus and tooltips. Windows are compared by handle, -ne would compare their
# children.
function DialogWindows {
    Windows | Where-Object {
        $_.Handle -ne $g.MainForm.ControlObject.Handle -and $_.Child -isnot [Gtk.Menu] -and
            $_.TypeHint -ne [Gdk.WindowTypeHint]::Tooltip
    }
}

function Dialogs { DialogWindows | ForEach-Object { $_.Title } }

function CloseDialogs { DialogWindows | ForEach-Object { $_.Close() } }

function Popups { @(Windows | Where-Object { $_.Child -is [Gtk.Menu] }).Count }

function HasGrab { $null -ne $g.MainForm.ControlObject.Group.CurrentGrab }

# a dialog has a label with the text
function HasText([string]$text) {
    [bool](DialogWindows | ForEach-Object { Find $_ { param($w) $w -is [Gtk.Label] -and $w.Text -eq $text } })
}

function MenuBar { ,$g.MainForm.Menu.ControlObject }

function Items($shell) {
    @($shell.Children | Where-Object { $_ -is [Gtk.MenuItem] -and $_ -isnot [Gtk.SeparatorMenuItem] })
}

# the item of a path of the main menu like "Tools > Scripts"
function Item([string]$path) {
    $shell = MenuBar
    $item = $null

    foreach ($name in $path -split ' > ') {
        if ($item) { $shell = $item.Submenu }
        $item = Items $shell | Where-Object { (Label $_) -eq $name } | Select-Object -First 1
        if (-not $item) { throw "no menu item $path" }
    }

    ,$item
}

function IsOpen([string]$path) { [bool](Item $path).Submenu.IsMapped }

function Origin($window) {
    [int]$x = 0
    [int]$y = 0
    $window.Window.GetOrigin([ref]$x, [ref]$y) | Out-Null
    $x
    $y
}

# "<x> <y> <width> <height> <x> <y> <title>": the rectangle of a widget relative to the surface of its window, of
# menus relative to the window of their menu bar, the position of that window, which is only known on X11, and its
# title
function Rect($widget) {
    $top = $widget.Toplevel
    $root = $top

    while ($root.Child -is [Gtk.Menu] -and $root.Child.AttachWidget) {
        $root = $root.Child.AttachWidget.Toplevel
    }

    [int]$x = 0
    [int]$y = 0
    $widget.TranslateCoordinates($top, 0, 0, [ref]$x, [ref]$y) | Out-Null
    $o = Origin $top
    $r = Origin $root
    $a = $widget.Allocation
    "$($x + $o[0] - $r[0]) $($y + $o[1] - $r[1]) $($a.Width) $($a.Height) $($r[0]) $($r[1]) $($root.Title)"
}

# the names of the items of an open menu with a submenu, that are sensitive and visible
function Submenus([string]$path) {
    foreach ($item in Items (Item $path).Submenu) {
        if (-not $item.Submenu -or -not $item.IsSensitive -or -not $item.IsMapped) { continue }
        [int]$x = 0
        [int]$y = 0
        $item.TranslateCoordinates($item.Toplevel, 0, 0, [ref]$x, [ref]$y) | Out-Null
        $center = $y + $item.Allocation.Height / 2
        if ($center -lt 0 -or $center -gt $item.Toplevel.Allocation.Height) { continue }
        Label $item
    }
}

function Button([string]$text) {
    foreach ($window in Windows) {
        $button = Find $window { param($w) $w -is [Gtk.Button] -and (Label $w) -eq $text }
        if ($null -ne $button) { return ,$button }
    }

    throw "no button $text"
}

# the position of the pointer relative to the surface of a window, the window has to be under it
function Pointer([string]$title) {
    $window = Window $title
    [int]$x = 0
    [int]$y = 0
    $under = $window.Display.DefaultSeat.Pointer.GetWindowAtPosition([ref]$x, [ref]$y)
    if (-not $under -or $under.Toplevel.Handle -ne $window.Window.Handle) { throw "the pointer isn't over $title" }
    [int]$ox = 0
    [int]$oy = 0
    $under.GetOrigin([ref]$ox, [ref]$oy) | Out-Null
    $r = Origin $window
    "$($x + $ox - $r[0]) $($y + $oy - $r[1])"
}

# renders the windows to /work/out/<name>-<n>.png
function Snap([string]$name) {
    $i = 0

    foreach ($window in Windows) {
        $a = $window.Allocation
        $surface = New-Object Cairo.ImageSurface([Cairo.Format]0, $a.Width, $a.Height)
        $context = New-Object Cairo.Context($surface)
        $window.Draw($context)
        $context.Dispose()
        $surface.WriteToPng("/work/out/$name-$i.png")
        $surface.Dispose()
        $i++
    }
}
'@

$references = 'System.Collections', 'System.ObjectModel', [Eto.Forms.UITimer].Assembly.Location,
    [StaxRip.PowerShell].Assembly.Location, [System.Management.Automation.PSObject].Assembly.Location

Add-Type -ReferencedAssemblies $references -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;

public static class UiTestLoop
{
    static string Dir, Functions;
    static List<Eto.Forms.UITimer> Timers = new List<Eto.Forms.UITimer>();

    public static void Start(string dir, string functions)
    {
        Dir = dir;
        Functions = functions;
        var timer = new Eto.Forms.UITimer { Interval = 0.05 };
        timer.Elapsed += (sender, e) => Run();
        Timers.Add(timer);
        timer.Start();
        Log("LOOP");
    }

    public static void Later(string code)
    {
        var timer = new Eto.Forms.UITimer { Interval = 0.2 };
        timer.Elapsed += (sender, e) =>
        {
            timer.Stop();
            try { StaxRip.PowerShell.Invoke(Functions + "\n" + code); }
            catch (Exception ex) { Log("LATER " + ex.Message.Replace("\n", " ")); }
        };
        Timers.Add(timer);
        timer.Start();
    }

    static void Log(string text) { File.AppendAllText(Path.Combine(Dir, "log"), text + "\n"); }

    static void Run()
    {
        var file = Path.Combine(Dir, "cmd.ps1");
        if (!File.Exists(file)) return;
        var code = File.ReadAllText(file);
        File.Delete(file);
        var id = code.Split('\n')[0].TrimStart('#').Trim();

        try
        {
            foreach (var output in StaxRip.PowerShell.Invoke(Functions + "\n" + code))
                if (output != null)
                    foreach (var line in output.ToString().Split('\n'))
                        Log("OUT " + id + " " + line);
        }
        catch (Exception ex)
        {
            Log("ERROR " + id + " " + ex.Message.Replace("\n", " "));
        }

        Log("DONE " + id);
    }
}
'@

[UiTestLoop]::Start($dir, $functions)
