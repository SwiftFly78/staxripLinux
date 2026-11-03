# Writes a script for every default filter profile of a frame server, as StaxRip generates it for a job, into a folder
# and appends them to its index.tsv: frames.ps1 <folder> <media folder> <AviSynth|VapourSynth> <clip>
# Every option of a $select:...$ macro is used by one of the variants of a profile, $enter_text:...$ gets the default
# its prompt names or a typical value.
$dir = $args[0]
$media = $args[1]
$engine = $args[2]
$clip = $args[3]
$g = [StaxRip.ShortcutModule]::g
$s = [StaxRip.ShortcutModule]::s
# no prompt to save the project before another source opens
$s.ApplicationExitMode = [StaxRip.ApplicationExitMode]::BypassProjectSaving
function P { [StaxRip.ShortcutModule]::p }
function Pump($seconds) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $seconds) { [Eto.Forms.Application]::Instance.RunIteration(); Start-Sleep -Milliseconds 10 }
}

function Options($macro) {
    $parts = @($macro -split ';')
    if ($parts[0] -like 'msg:*') { $parts = @($parts | Select-Object -Skip 1) }
    @($parts | Where-Object { $_ -ne '' } | ForEach-Object { if ($_.Contains('|')) { $_.Substring($_.IndexOf('|') + 1) } else { $_ } })
}

function Text($prompt, $before) {
    if ($prompt -match '\bDefault: ([-0-9.]+)') { return $Matches[1] }
    switch -Regex ($prompt) {
        'Pixels' { return '8' }
        'Numerator|Num Value|NewNum|New Num' { return '30000' }
        'Denominator|Den Value|NewDen|New Den' { return '1001' }
        'Sharpness' { return '0.5' }
        'Threads|Cores' { return '2' }
        'Maximum Memory' { return '2048' }
        'Filter Name' { return 'DEFAULT_MT_MODE' }
        'Mode you wish' { return '2' }
        'Format' { if ($before -match 'ConvertTo$') { return 'YV24' } else { return 'YUV444P8' } }
    }
    throw "no test value for the prompt: $prompt"
}

function Variants($code) {
    $selects = [regex]::Matches($code, '\$select:([^$]*)\$')
    $count = 1
    foreach ($m in $selects) { $count = [Math]::Max($count, (Options $m.Groups[1].Value).Count) }

    for ($k = 0; $k -lt $count; $k++) {
        $variant = [regex]::Replace($code, '\$select:([^$]*)\$', { param($m) $o = Options $m.Groups[1].Value; $o[$k % $o.Count] })
        $variant = [regex]::Replace($variant, '\$enter_text:([^$]*)\$', {
            param($m) Text $m.Groups[1].Value $variant.Substring(0, $m.Index)
        })
        $variant.Replace('$browse_file$', (Join-Path $media 'identity.cube'))
    }
}

$index = Join-Path $dir 'index.tsv'
$n = 0
$profiles = if ($engine -eq 'AviSynth') { $s.AviSynthProfiles } else { $s.VapourSynthProfiles }
$ext = if ($engine -eq 'AviSynth') { 'avs' } else { 'vpy' }
$g.MainForm.LoadFilterSetup(($s.FilterSetupProfiles | Where-Object Name -eq $engine | Select-Object -First 1))
$source = ($profiles | Where-Object Name -eq 'Source').Filters |
    Where-Object { $_.Name -eq $(if ($engine -eq 'AviSynth') { 'FFVideoSource' } else { 'ffms2' }) } | Select-Object -First 1
(P).Script.SetFilter('Source', $source.Path, $source.Script)
$g.MainForm.OpenVideoSourceFiles([string[]](Join-Path $media $clip))
Pump 1
$base = (P).Script.GetNewScript()
$tenBit = $clip -like '*10*'

foreach ($category in $profiles) {
    if ($category.Name -eq 'Source') { continue }
    # the 10-bit clip tests the color filters, which have the HDR conversions
    if ($tenBit -and $category.Name -ne 'Color') { continue }

    foreach ($filter in $category.Filters) {
        $k = 0
        try {
            $codes = @(Variants $filter.Script)
        } catch {
            Add-Content -LiteralPath $index -Value "`t$engine $($category.Name) | $($filter.Path)`t$($_.Exception.Message)"
            continue
        }

        foreach ($code in $codes) {
            $k++
            $n++
            $name = "$engine $($category.Name) | $($filter.Path)" + $(if ($tenBit) { ' (10 bit)' } else { '' }) + " #$k"
            $file = Join-Path $dir ("$engine-$([IO.Path]::GetFileNameWithoutExtension($clip))-{0:D4}.$ext" -f $n)

            try {
                # not the script of the project, the main window would evaluate every change
                $test = $base.GetNewScript()
                $test.SetFilter($category.Name, $filter.Path, $code)
                Set-Content -LiteralPath $file -Value $test.GetFullScript()
                Add-Content -LiteralPath $index -Value "$file`t$name"
            } catch {
                Add-Content -LiteralPath $index -Value "`t$name`t$($_.Exception.Message -replace '\s+', ' ')"
            }
        }
    }
}

[Environment]::Exit(0)
