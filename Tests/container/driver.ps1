# Runs one pipeline spec of pipelines.py in StaxRip: driver.ps1 <spec> <result>
# Spec keys: sources, target, engine, sourcefilter, video, muxer, audio (profile names), audioEncoder, code (PowerShell
# run after the source is open), noencode, timeout. The result gets lines like "TARGET: ..." and ends with DONE.
$spec = Get-Content -Raw $args[0] | ConvertFrom-Json
$out = $args[1]
$g = [StaxRip.ShortcutModule]::g
$s = [StaxRip.ShortcutModule]::s
# no prompt to save the project before another source opens
$s.ApplicationExitMode = [StaxRip.ApplicationExitMode]::BypassProjectSaving
function Out($line) { Add-Content -LiteralPath $out -Value $line }
function P { [StaxRip.ShortcutModule]::p }
function Pump($seconds) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $seconds) { [Eto.Forms.Application]::Instance.RunIteration(); Start-Sleep -Milliseconds 10 }
}
# sets the option of the encoder with the switch, also options without a property
function SetSwitch($switch, $value) {
    $item = (P).VideoEncoder.Params.Items | Where-Object { $_.Switch -eq $switch } | Select-Object -First 1
    if ($item) { $item.Value = $value }
}
function Find($list, $name, $kind) {
    $r = $list | Where-Object { $_.Name -eq $name } | Select-Object -First 1
    if (-not $r) { throw "$kind profile not found: $name" }
    $r
}

# a message box of an error blocks the job, it's reported and ends StaxRip
$script:watch = New-Object Eto.Forms.UITimer
$script:watch.Interval = 1
$script:watch.add_Elapsed({
    foreach ($w in [Eto.Forms.Application]::Instance.Windows) {
        if ($w.GetType().Name -eq 'TaskDialogForm' -and $w.Visible) {
            $texts = @($w.Children | Where-Object { $_.Text -is [string] -and $_.Text.Trim() } | ForEach-Object { $_.Text.Trim() })
            Out ("DIALOG: " + $w.Title + " || " + ($texts -join " | ") -replace "`r?`n", " ")
            [Environment]::Exit(1)
        }
    }
})
$script:watch.Start()

try {
    if ($spec.engine) {
        $g.MainForm.LoadFilterSetup((Find $s.FilterSetupProfiles $spec.engine 'filter setup'))
    }

    if ($spec.sourcefilter) {
        $profiles = if ($spec.engine -eq 'VapourSynth') { $s.VapourSynthProfiles } else { $s.AviSynthProfiles }
        $f = ($profiles | Where-Object Name -eq 'Source').Filters | Where-Object Name -eq $spec.sourcefilter |
            Select-Object -First 1
        if (-not $f) { throw "source filter not found: $($spec.sourcefilter)" }
        (P).Script.SetFilter('Source', $f.Path, $f.Script)
    }

    if ($spec.video) { $g.LoadVideoEncoder((Find $s.VideoEncoderProfiles $spec.video 'video')) }
    $g.MainForm.OpenVideoSourceFiles([string[]]$spec.sources)
    Pump 1
    if ($spec.video) { $g.LoadVideoEncoder((Find $s.VideoEncoderProfiles $spec.video 'video')) }
    if ($spec.muxer) { (P).VideoEncoder.LoadMuxer((Find $s.MuxerProfiles $spec.muxer 'muxer')) }

    if ($spec.audio) {
        for ($i = 0; $i -lt (P).AudioTracks.Count; $i++) {
            # the template disables tracks with No Audio, they stay disabled
            if ((P).AudioTracks[$i].AudioProfile.GetType().Name -eq 'NullAudioProfile') { continue }
            $g.LoadAudioProfile((Find $s.AudioProfiles $spec.audio 'audio'), $i)
            if ($spec.audioEncoder) {
                $a = (P).AudioTracks[$i].AudioProfile
                $a.Params.Encoder = [StaxRip.GuiAudioEncoder]$spec.audioEncoder
                # like the audio dialog, which limits the quality to the range of the encoder
                if ($a.Params.Codec -eq 'AAC' -and $spec.audioEncoder -in 'fdkaac', 'ffmpeg') {
                    $a.Params.Quality = [Math]::Min([Math]::Max($a.Params.Quality, 1), 5)
                }
            }
        }
    }

    if ($spec.code) { Invoke-Expression $spec.code }
    if ($spec.target) {
        # the extension of the muxer, StaxRip writes the target with it
        $target = [IO.Path]::ChangeExtension($spec.target, (P).VideoEncoder.Muxer.OutputExtFull)
        $g.DefaultCommands.SetTargetFile($target)
        $g.MainForm.tbTargetFile.Text = $target
    }
    $g.MainForm.UpdateFilters()
    Pump 1
    $g.MainForm.Assistant() | Out-Null
    Out ("ASSISTANT: " + $g.MainForm.NextStep.TitleLabel.Text + " || " + $g.MainForm.NextStep.MessageLabel.Text)
    Out ("SOURCE: " + (P).SourceFile)
    Out ("ENCODER: " + (P).VideoEncoder.Name + " | MUXER: " + (P).VideoEncoder.Muxer.Name)
    Out ("CMDL: " + (P).VideoEncoder.GetCommandLine($true, $true))
    Out ("SCRIPT: " + ((P).Script.GetFullScript() -replace "`r?`n", " ## "))

    for ($i = 0; $i -lt (P).AudioTracks.Count; $i++) {
        $t = (P).AudioTracks[$i]
        $encoder = if ($t.AudioProfile.GetType().Name -eq 'GUIAudioProfile') { $t.AudioProfile.GetEncoder() } else { '' }
        if ($t.AudioProfile.File) { Out ("AUDIO$($i): " + $t.AudioProfile.Name + " | " + $encoder + " | " + $t.AudioProfile.File) }
    }

    Out ("TARGET: " + (P).TargetFile)
    if ($spec.noencode) { Out "DONE"; [Environment]::Exit(0) }
    $g.MainForm.StartEncoding()
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while (-not $g.IsJobProcessing -and $sw.Elapsed.TotalSeconds -lt 20) { Pump 0.1 }
    Out ("STARTED: " + $g.IsJobProcessing)
    while ($g.IsJobProcessing -and $sw.Elapsed.TotalSeconds -lt $spec.timeout) { Pump 0.2 }
    Pump 2
    Out ("FINISHED: " + (-not $g.IsJobProcessing) + " after " + [int]$sw.Elapsed.TotalSeconds + "s")
    Out "DONE"
} catch {
    Out ("EXCEPTION: " + $_.Exception.ToString() + " " + $_.ScriptStackTrace -replace "`r?`n", " ")
}

[Environment]::Exit(0)
