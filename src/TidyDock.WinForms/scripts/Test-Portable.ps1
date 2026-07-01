param(
    [string]$PortableRoot = "",
    [switch]$SkipLaunch
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PortableRoot)) {
    $PortableRoot = Join-Path $projectRoot "dist\TidyDock-winforms-portable"
}

$zipPath = Join-Path (Split-Path -Parent $PortableRoot) "TidyDock-winforms-portable.zip"
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
    Assert-True (Test-Path (Join-Path $PortableRoot $file)) "Missing portable file: $file"
}

Write-Host "Checking portable zip..."
Assert-True (Test-Path $zipPath) "Portable zip not found: $zipPath"

Write-Host "Checking version info..."
$version = (Get-Item -LiteralPath $exePath).VersionInfo
Assert-True ($version.ProductName -eq "TidyDock") "Unexpected product name: $($version.ProductName)"
Assert-True ($version.FileVersion -eq "0.2.0.0") "Unexpected file version: $($version.FileVersion)"

if (-not $SkipLaunch) {
    Write-Host "Checking launch smoke..."
    Get-Process -Name TidyDock -ErrorAction SilentlyContinue | Stop-Process -Force
    $process = Start-Process -FilePath $exePath -PassThru
    Start-Sleep -Seconds 3
    $running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
    Assert-True ($null -ne $running) "TidyDock exited during launch smoke."
    Stop-Process -Id $process.Id -Force
}

Write-Host "Portable verification passed."
