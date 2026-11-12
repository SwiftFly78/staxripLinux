# [Documentation](../../README.md) / [Guides](../README.md) / PowerShell Scripts

Ready to use scripts for common tasks. Save a script as `.ps1` file in the `Scripts` folder of the settings
(`Tools > Folders > Scripts`) to run it from the *Scripts* menu, or in `Scripts/Auto Load` to run it whenever
StaxRipLinux starts. [PowerShell Scripting](../../Usage/PowerShell.md) explains how scripts work.

## Show the media info of the source in the log

Runs after a source was opened and writes the format, the HDR format and the scan type into the log:

```powershell
# Scripts/Auto Load/Log source.ps1
Register-ObjectEvent -InputObject ([ShortcutModule]::g) -EventName AfterSourceLoaded -Action {
    $p = [ShortcutModule]::p
    $file = $p.FirstOriginalSourceFile
    $info = 'Format: {0}, HDR: {1}, Scan type: {2}' -f
        [MediaInfo]::GetVideo($file, 'Format'),
        [MediaInfo]::GetVideo($file, 'HDR_Format'),
        [MediaInfo]::GetVideo($file, 'ScanType')
    [MainModule]::Log.WriteHeader('Source summary')
    [MainModule]::Log.WriteLine($info)
} | Out-Null
```

## Use a lower quality value for sources below 1080p

Small sources need relatively more bits per pixel. This script lowers the CRF of x265 for them:

```powershell
# Scripts/Auto Load/SD quality.ps1
Register-ObjectEvent -InputObject ([ShortcutModule]::g) -EventName AfterSourceLoaded -Action {
    $p = [ShortcutModule]::p

    if ($p.SourceHeight -lt 1080 -and $p.VideoEncoder.GetType().Name -eq 'x265Enc') {
        $p.VideoEncoder.Params.Quant.Value = 18
    }
} | Out-Null
```

## Add all videos of a folder as jobs

Asks for a folder and adds a job for every MKV file in it, with the current template:

```powershell
# Scripts/Add folder as jobs.ps1
$dialog = New-Object Eto.Forms.SelectFolderDialog
$dialog.Title = 'Select a folder with MKV files'

if ($dialog.ShowDialog([ShortcutModule]::g.MainForm) -eq 'Ok') {
    foreach ($file in Get-ChildItem -Path $dialog.Directory -Filter *.mkv) {
        [ShortcutModule]::g.MainForm.AddBatchJob($file.FullName)
    }

    [ShortcutModule]::g.MainForm.ShowJobsDialog()
}
```

## Notify when all jobs are done

Shows a desktop notification with `notify-send` after the last job:

```powershell
# Scripts/AfterJobsProcessed.ps1
notify-send 'StaxRipLinux' 'All jobs are done.'
```

The same works as [event command](../../Usage/Events.md) with `ExecuteCommandLine`.

> [!TIP]
> To find the names of properties, list them in the PowerShell of StaxRipLinux, for example with a script that runs
> `[MainModule]::MsgInfo('Properties', (([ShortcutModule]::p | Get-Member -MemberType Property).Name -join ', '))`.
