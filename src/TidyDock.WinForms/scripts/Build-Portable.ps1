param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $projectRoot "TidyDock.WinForms.csproj"
$distRoot = Join-Path $projectRoot "dist"
$portableRoot = Join-Path $distRoot "TidyDock-winforms-portable"
$zipPath = Join-Path $distRoot "TidyDock-winforms-portable.zip"
$publishRoot = Join-Path $projectRoot "bin\$Configuration\net8.0-windows\$Runtime\publish"

if (Test-Path $portableRoot) {
    Remove-Item -LiteralPath $portableRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $portableRoot | Out-Null
New-Item -ItemType Directory -Force -Path $distRoot | Out-Null

& dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

Copy-Item -LiteralPath (Join-Path $publishRoot "TidyDock.exe") -Destination (Join-Path $portableRoot "TidyDock.exe") -Force
Copy-Item -LiteralPath (Join-Path $projectRoot "..\TidyDock\assets\TidyDock.ico") -Destination (Join-Path $portableRoot "TidyDock.ico") -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Install-CurrentUser.ps1") -Destination (Join-Path $portableRoot "Install-CurrentUser.ps1") -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Uninstall-CurrentUser.ps1") -Destination (Join-Path $portableRoot "Uninstall-CurrentUser.ps1") -Force

@"
TidyDock v0.2 WinForms preview

This is the Windows-only low-resource rewrite. It uses a separate config path:
%APPDATA%\TidyDock\winforms\settings.json

Run:
  TidyDock.exe

Install for current user:
  powershell -ExecutionPolicy Bypass -File .\Install-CurrentUser.ps1 -Launch
"@ | Set-Content -LiteralPath (Join-Path $portableRoot "README.txt") -Encoding UTF8

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -LiteralPath $portableRoot -DestinationPath $zipPath -Force

Write-Host "Portable package:"
Write-Host $portableRoot
Write-Host $zipPath
