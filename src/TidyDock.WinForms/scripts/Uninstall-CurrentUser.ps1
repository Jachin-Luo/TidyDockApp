$ErrorActionPreference = "Stop"

Get-Process -Name TidyDock -ErrorAction SilentlyContinue | Stop-Process -Force

$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
Remove-ItemProperty -Path $runKey -Name "TidyDock.WinForms" -ErrorAction SilentlyContinue

$target = Join-Path $env:LOCALAPPDATA "Programs\TidyDock.WinForms"
if (Test-Path $target) {
    Remove-Item -LiteralPath $target -Recurse -Force
}

Write-Host "TidyDock WinForms removed for current user."
