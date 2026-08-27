# Crea un acceso directo en el Escritorio hacia AirLyrics.exe con el icono oficial
$wshShell = New-Object -ComObject WScript.Shell
$desktopPath = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Desktop)
$targetExe = (Resolve-Path ".\publish\AirLyrics.exe").Path
$iconFile = (Resolve-Path ".\src\AirLyrics.App\app.ico").Path

$shortcutPath = Join-Path $desktopPath "AirLyrics.lnk"
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = [System.IO.Path]::GetDirectoryName($targetExe)
$shortcut.IconLocation = "$iconFile, 0"
$shortcut.Description = "AirLyrics - Letras de Spotify en tiempo real con modo fantasma"
$shortcut.Save()

Write-Host "✅ Acceso directo creado en tu Escritorio: $shortcutPath" -ForegroundColor Green
