param(
    [switch]$Launch
)

$ErrorActionPreference = "Stop"

$source = Split-Path -Parent $MyInvocation.MyCommand.Path
$target = Join-Path $env:LOCALAPPDATA "Programs\TidyDock.WinForms"

New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -LiteralPath (Join-Path $source "*") -Destination $target -Recurse -Force

$exe = Join-Path $target "TidyDock.exe"
if ($Launch) {
    Start-Process -FilePath $exe
}

Write-Host "Installed TidyDock WinForms to:"
Write-Host $target
