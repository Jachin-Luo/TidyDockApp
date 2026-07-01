param(
    [switch]$StartWithWindows,
    [switch]$Launch
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceExe = Join-Path $scriptDir "TidyDock.exe"
if (-not (Test-Path $sourceExe)) {
    $sourceExe = Join-Path (Split-Path -Parent $scriptDir) "bin\Release\TidyDock.exe"
}
if (-not (Test-Path $sourceExe)) {
    throw "TidyDock.exe not found next to installer or in bin\Release."
}

$installRoot = Join-Path $env:LOCALAPPDATA "Programs\TidyDock"
$installExe = Join-Path $installRoot "TidyDock.exe"
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\TidyDock"
$shortcutPath = Join-Path $startMenuDir "TidyDock.lnk"
$uninstallShortcutPath = Join-Path $startMenuDir "Uninstall TidyDock.lnk"
$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"

Get-Process -Name TidyDock -ErrorAction SilentlyContinue | Stop-Process -Force

New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
Copy-Item -LiteralPath $sourceExe -Destination $installExe -Force

$sourceReadme = Join-Path $scriptDir "README.txt"
if (Test-Path $sourceReadme) {
    Copy-Item -LiteralPath $sourceReadme -Destination (Join-Path $installRoot "README.txt") -Force
}

$sourceUninstall = Join-Path $scriptDir "Uninstall-CurrentUser.ps1"
if (Test-Path $sourceUninstall) {
    Copy-Item -LiteralPath $sourceUninstall -Destination (Join-Path $installRoot "Uninstall-CurrentUser.ps1") -Force
}

New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null
$shell = New-Object -ComObject WScript.Shell

$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $installExe
$shortcut.WorkingDirectory = $installRoot
$shortcut.Description = "TidyDock"
$shortcut.Save()

$uninstallScript = Join-Path $installRoot "Uninstall-CurrentUser.ps1"
if (Test-Path $uninstallScript) {
    $uninstallShortcut = $shell.CreateShortcut($uninstallShortcutPath)
    $uninstallShortcut.TargetPath = "powershell.exe"
    $uninstallShortcut.Arguments = "-ExecutionPolicy Bypass -File `"$uninstallScript`""
    $uninstallShortcut.WorkingDirectory = $installRoot
    $uninstallShortcut.Description = "Uninstall TidyDock"
    $uninstallShortcut.Save()
}

if ($StartWithWindows) {
    Set-ItemProperty -Path $runKey -Name "TidyDock" -Value "`"$installExe`""
}

if ($Launch) {
    Start-Process -FilePath $installExe
}

Write-Host "TidyDock installed:"
Write-Host $installExe
Write-Host "Start Menu:"
Write-Host $shortcutPath
