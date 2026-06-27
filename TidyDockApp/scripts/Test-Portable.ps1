param(
    [string]$PortableRoot = "",
    [switch]$SkipLaunch
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PortableRoot)) {
    $PortableRoot = Join-Path $projectRoot "dist\TidyDock-portable"
}

$zipPath = Join-Path (Split-Path -Parent $PortableRoot) "TidyDock-portable.zip"
$exePath = Join-Path $PortableRoot "TidyDock.exe"
$requiredFiles = @(
    "TidyDock.exe",
    "TidyDock.ico",
    "README.txt",
    "Install-CurrentUser.ps1",
    "Uninstall-CurrentUser.ps1"
)

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

Write-Host "Checking portable directory..."
Assert-True (Test-Path $PortableRoot) "Portable directory not found: $PortableRoot"
foreach ($file in $requiredFiles) {
    $path = Join-Path $PortableRoot $file
    Assert-True (Test-Path $path) "Missing portable file: $file"
}

Write-Host "Checking portable zip..."
Assert-True (Test-Path $zipPath) "Portable zip not found: $zipPath"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    foreach ($file in $requiredFiles) {
        $entryName = "TidyDock-portable/$file"
        $entry = $zip.Entries | Where-Object { $_.FullName.Replace("\", "/") -eq $entryName } | Select-Object -First 1
        Assert-True ($null -ne $entry) "Missing zip entry: $entryName"
    }
}
finally {
    $zip.Dispose()
}

Write-Host "Checking icon resource..."
Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Icon]::ExtractAssociatedIcon($exePath)
Assert-True ($null -ne $icon) "Executable icon could not be extracted."
try {
    Assert-True ($icon.Width -gt 0 -and $icon.Height -gt 0) "Executable icon has invalid dimensions."
}
finally {
    $icon.Dispose()
}

Write-Host "Checking version info..."
$version = (Get-Item -LiteralPath $exePath).VersionInfo
Assert-True ($version.ProductName -eq "TidyDock") "Unexpected product name: $($version.ProductName)"
Assert-True ($version.FileVersion -eq "0.1.0.0") "Unexpected file version: $($version.FileVersion)"

Write-Host "Checking source text for common mojibake markers..."
$markers = @(0x9352, 0x93c2, 0x7ed7, 0x9225, 0x8133, 0x59dd, 0x6905, 0x5997, 0x7481, 0x9358, 0x7f03, 0x6f70, 0x9881, 0x7c28, 0x93c8) |
    ForEach-Object { [string][char]$_ }
$sourceFiles = Get-ChildItem -LiteralPath $projectRoot -Recurse -Include *.cs,*.md,*.ps1,*.txt |
    Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\|\\dist\\" }
$matches = $sourceFiles | Select-String -SimpleMatch $markers
Assert-True (($matches | Measure-Object).Count -eq 0) ("Possible mojibake markers found:" + [Environment]::NewLine + ($matches | Out-String))

if (-not $SkipLaunch) {
    Write-Host "Checking launch and single-instance behavior..."
    Get-Process -Name TidyDock -ErrorAction SilentlyContinue | Stop-Process -Force
    $first = Start-Process -FilePath $exePath -PassThru
    Start-Sleep -Seconds 2
    $second = Start-Process -FilePath $exePath -PassThru
    Start-Sleep -Seconds 2
    $processes = @(Get-Process -Name TidyDock -ErrorAction SilentlyContinue)
    Assert-True ($processes.Count -eq 1) "Expected one TidyDock process, found $($processes.Count)."
    Assert-True ($processes[0].Path -eq $exePath) "Running process path mismatch: $($processes[0].Path)"

    Write-Host "Checking local config generation..."
    $configPath = Join-Path $env:APPDATA "TidyDock\config\settings.json"
    Assert-True (Test-Path $configPath) "Config file was not generated: $configPath"
}

Write-Host "Portable verification passed."
