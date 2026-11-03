# Ends the first start of StaxRip, which saves the settings and confirms the changelog
Set-Content -LiteralPath $args[0] -Value "DONE"
[StaxRip.ShortcutModule]::g.SaveSettings()
[Environment]::Exit(0)
