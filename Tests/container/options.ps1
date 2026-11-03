# Checks that the bundled encoders still know every switch the options of their StaxRip profiles can pass, a new
# version doesn't name the switches it removed or renamed in its help and rejects them: options.ps1 <results file>
# A missing switch is a result of its own, so a known one doesn't hide new ones.
$out = $args[0]
$s = [StaxRip.ShortcutModule]::s

function Result($status, $name, $detail) {
    $detail = "$detail" -replace '\s+', ' '
    Add-Content -LiteralPath $out -Value "$status`t$name`t$($detail.Substring(0, [Math]::Min(500, $detail.Length)))"
}

# some switches work without the help naming them, a removed one makes the encoder reject it as unknown
function Rejected($path, $switch) {
    $text = & timeout 60 $path $switch 1 2>&1 | Out-String
    return $text -match ('(?i)(unrecognized|unknown) option:? ''?-*' + [regex]::Escape($switch.TrimStart('-')) + '\b')
}

$done = @{}

foreach ($enc in $s.VideoEncoderProfiles) {
    $params = $enc.Params
    if (-not $params -or -not $params.Items) { continue }
    $pack = $params.Package
    $name = "options " + $enc.Name

    if (-not $pack -or -not $pack.Path) { Result 'skip' $name 'the encoder isn''t bundled'; continue }
    # the profiles of an encoder share its options
    $key = $params.GetType().FullName + '|' + $pack.Name
    if ($done[$key]) { continue }
    $done[$key] = $true

    # the help switch of the package, and the ones of the full help, which some tools show separately
    $help = $pack.CreateHelpfile()
    foreach ($switch in '--fullhelp', '--full-help', '--longhelp') {
        $help += & $pack.Path $switch 2>&1 | Out-String
    }
    if ($pack.Name -eq 'ffmpeg') { $help += & $pack.Path -hide_banner -h full 2>&1 | Out-String }
    if (-not $help.Trim()) { Result 'fail' $name 'no help'; continue }
    # --[no-]amp and --(no-)amp stand for --amp and --no-amp
    $help = [regex]::Replace($help, '--[\[\(]no-[\]\)]([\w-]+)', '--$1 --no-$1')

    # the help switch only finds the option in the help, it's never passed
    $missing = foreach ($item in $params.Items) {
        foreach ($switch in @($item.Switch, $item.NoSwitch) + @($item.Switches) | Where-Object { $_ }) {
            $plain = ("$switch" -split '\s+')[0] -replace ':[a-z0-9]+$', ''
            if ($plain -notmatch '^-') { continue }
            if ($help -notmatch ('(^|[\s,\[\(/|])' + [regex]::Escape($plain) + '($|[\s=,:<\[\]\)/|])')) { $plain }
        }
    }

    $missing = @($missing | Sort-Object -Unique | Where-Object { Rejected $pack.Path $_ })
    foreach ($switch in $missing) { Result 'fail' "$name $switch" "$($pack.Name) rejects it as unknown" }
    if (-not $missing.Count) { Result 'pass' $name '' }
}

[Environment]::Exit(0)
