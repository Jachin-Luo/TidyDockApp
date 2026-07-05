param(
    [switch]$Launch
)

$ErrorActionPreference = "Stop"

$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $env:LOCALAPPDATA "Programs\TidyDock.WinForms"

New-Item -ItemType Directory -Force -Path $target | Out-Null
Get-ChildItem -LiteralPath $source | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force
}

$exe = Join-Path $target "TidyDock.exe"
$startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\TidyDock"
New-Item -ItemType Directory -Force -Path $startMenu | Out-Null

$shell = New-Object -ComObject WScript.Shell
$appShortcut = $shell.CreateShortcut((Join-Path $startMenu "TidyDock.lnk"))
$appShortcut.TargetPath = $exe
$appShortcut.WorkingDirectory = $target
$appShortcut.IconLocation = Join-Path $target "TidyDock.ico"
$appShortcut.Save()

$uninstallName = "$([char]0x5378)$([char]0x8f7d) TidyDock.lnk"
$uninstallShortcut = $shell.CreateShortcut((Join-Path $startMenu $uninstallName))
$uninstallShortcut.TargetPath = "powershell.exe"
$uninstallShortcut.Arguments = "-ExecutionPolicy Bypass -File `"$target\Uninstall-CurrentUser.ps1`""
$uninstallShortcut.WorkingDirectory = $target
$uninstallShortcut.IconLocation = Join-Path $target "TidyDock.ico"
$uninstallShortcut.Save()

$uninstallAndRemoveDataName = "$([char]0x5378)$([char]0x8f7d)$([char]0x5e76)$([char]0x6e05)$([char]0x9664)$([char]0x914d)$([char]0x7f6e) TidyDock.lnk"
$uninstallAndRemoveDataShortcut = $shell.CreateShortcut((Join-Path $startMenu $uninstallAndRemoveDataName))
$uninstallAndRemoveDataShortcut.TargetPath = "powershell.exe"
$uninstallAndRemoveDataShortcut.Arguments = "-ExecutionPolicy Bypass -File `"$target\Uninstall-CurrentUser.ps1`" -RemoveUserData"
$uninstallAndRemoveDataShortcut.WorkingDirectory = $target
$uninstallAndRemoveDataShortcut.IconLocation = Join-Path $target "TidyDock.ico"
$uninstallAndRemoveDataShortcut.Save()

if ($Launch) {
    Start-Process -FilePath $exe
}

Write-Host "Installed TidyDock WinForms to:"
Write-Host $target
