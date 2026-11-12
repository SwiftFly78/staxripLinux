# [Documentation](../README.md) / [Usage](README.md) / PowerShell Scripting

StaxRipLinux contains PowerShell 7, which can automate StaxRipLinux itself: change the project, react to events, add
jobs or show dialogs. Scripts have access to the objects of StaxRipLinux:

| Object | Description |
|---|---|
| `[ShortcutModule]::g` | Global functions and events, like `g.MainForm` (the main window) and `g.DefaultCommands` (all [commands](Commands.md)) |
| `[ShortcutModule]::p` | The current project: source, script, encoder, audio tracks, options |
| `[ShortcutModule]::s` | The settings |

The types of StaxRipLinux, like `MediaInfo`, `VideoFilter` or `TaskDialog`, can be used without namespace.

## Running scripts

- **Scripts menu**: `.ps1` files in the `Scripts` folder of the settings (`Tools > Folders > Scripts`) appear in the
  *Scripts* menu of the main window and run when they're chosen.
- **Auto Load**: scripts in `Scripts/Auto Load` run when StaxRipLinux starts, for example to register event handlers.
- **Events**: a script named after an [event](Events.md), like `AfterJobProcessed.ps1` in the `Scripts` folder, runs
  when the event is raised.
- **Commands**: `ExecutePowerShellFile` and `ExecutePowerShellCode` run scripts and code, in menus,
  [event commands](Events.md) and on the [command line](Command-Line-Interface.md):

  ```sh
  staxrip "-ExecutePowerShellFile:/path/to/script.ps1"
  ```

  With *Use External Shell*, `ExecutePowerShellCode` runs the code in a terminal with `pwsh` instead, which needs
  PowerShell to be installed.
- **Macros**: `%eval:expression%` evaluates a PowerShell expression in any text that supports
  [macros](Macros.md#function-macros).

`Tools > Advanced > PowerShell Terminal` opens `pwsh` in a terminal with the macros as environment variables, if
PowerShell is installed.

## Examples

Show a message with the source file:

```powershell
$p = [ShortcutModule]::p
[MainModule]::MsgInfo('Source', $p.SourceFile)
```

Add a denoiser to every source that is opened, saved as `Scripts/Auto Load/Denoise.ps1`:

```powershell
Register-ObjectEvent -InputObject ([ShortcutModule]::g) -EventName AfterSourceLoaded -Action {
    $p = [ShortcutModule]::p

    if ($p.Script.IsVapourSynth) {
        $p.Script.SetFilter('Noise', 'DFTTest', 'clip = core.dfttest.DFTTest(clip, sigma=2, tbsize=3)')
    }
} | Out-Null
```

Add a deinterlacer for interlaced sources:

```powershell
Register-ObjectEvent -InputObject ([ShortcutModule]::g) -EventName AfterSourceLoaded -Action {
    $p = [ShortcutModule]::p
    $scanType = [MediaInfo]::GetVideo($p.FirstOriginalSourceFile, 'ScanType')

    if ($scanType -eq 'Interlaced' -and $p.Script.IsVapourSynth) {
        $p.Script.SetFilter('Field', 'QTGMC', "clip = havsfunc.QTGMC(clip, TFF=True, Preset='Medium')")
    }
} | Out-Null
```

Ask with a dialog:

```powershell
$td = New-Object TaskDialog[string]
$td.Title = 'Select an option.'
$td.AddCommand('Option A', 'a')
$td.AddCommand('Option B', 'b')
$result = $td.Show()
$td.Dispose()

if ($result -eq 'a') {
    [MainModule]::MsgInfo('Option A was selected.')
}
```

Open a source, set the encoder and add a job, for example from the command line:

```powershell
$g = [ShortcutModule]::g
$s = [ShortcutModule]::s
$g.MainForm.OpenVideoSourceFiles([string[]]@('/home/user/Videos/Sample.mkv'))
$g.LoadVideoEncoder(($s.VideoEncoderProfiles | Where-Object { $_.Name -eq 'x265' } | Select-Object -First 1))
$g.MainForm.AddJob($false, -1)
```

> [!TIP]
> The source code of StaxRipLinux shows all properties and methods the objects have. PowerShell itself lists them too,
> for example `[ShortcutModule]::p | Get-Member`.
