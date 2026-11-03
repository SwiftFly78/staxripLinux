# Writes the profiles and packages of StaxRip as JSON for the test generators: inventory.ps1 <file>
$s = [StaxRip.ShortcutModule]::s

function Try($block) { try { & $block } catch { $null } }

$encoders = foreach ($e in $s.VideoEncoderProfiles) {
    [ordered]@{ name = $e.Name; type = $e.GetType().Name; ext = (Try { $e.OutputExt }); muxer = $e.Muxer.Name }
}

$muxers = foreach ($m in $s.MuxerProfiles) {
    [ordered]@{
        name = $m.Name; type = $m.GetType().Name; ext = (Try { $m.OutputExt })
        encoders = @($encoders | Where-Object { $_.ext -and (Try { $m.IsSupported($_.ext) }) } | ForEach-Object { $_.name })
    }
}

$candidates = @{ ffmpeg = 'ffmpeg'; fdkaac = 'fdkaac'; opusenc = 'OpusEnc' }

$audio = foreach ($a in $s.AudioProfiles) {
    $item = [ordered]@{ name = $a.Name; type = $a.GetType().Name; codec = $null; encoders = @() }

    if ($a.GetType().Name -eq 'GUIAudioProfile') {
        $item.codec = $a.Params.Codec.ToString()
        $item.encoders = @(foreach ($c in $candidates.Keys) {
            $enum = [StaxRip.GuiAudioEncoder]$c
            $pack = [StaxRip.Package]::Items.Values | Where-Object Name -eq $candidates[$c] | Select-Object -First 1
            if ($a.GetEncoder($enum) -eq $enum -and $pack -and $pack.Path) { $c }
        })
    }

    $item
}

function Filters($categories) {
    foreach ($cat in $categories) {
        foreach ($f in $cat.Filters) { [ordered]@{ category = $cat.Name; name = $f.Path; script = $f.Script } }
    }
}

$packages = foreach ($p in [StaxRip.Package]::Items.Values) {
    [ordered]@{
        name = $p.Name; type = $p.GetType().Name; file = $p.Filename; path = $p.Path; status = $p.GetStatus()
        linux = $p.IsLinuxSupported; avs = @($p.AvsFilterNames | Where-Object { $_ }); vs = @($p.VsFilterNames | Where-Object { $_ })
    }
}

$demuxers = foreach ($d in $s.Demuxers) {
    [ordered]@{ name = $d.Name; inputs = @($d.InputExtensions); active = $d.Active }
}

[ordered]@{
    encoders = @($encoders); muxers = @($muxers); audio = @($audio); demuxers = @($demuxers)
    setups = @($s.FilterSetupProfiles | ForEach-Object { $_.Name })
    filters = [ordered]@{ AviSynth = @(Filters $s.AviSynthProfiles); VapourSynth = @(Filters $s.VapourSynthProfiles) }
    packages = @($packages)
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $args[0]

[Environment]::Exit(0)
