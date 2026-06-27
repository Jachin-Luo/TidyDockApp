param(
    [switch]$KeepData
)

$ErrorActionPreference = "Stop"

$installRoot = Join-Path $env:LOCALAPPDATA "Programs\TidyDock"
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\TidyDock"
$dataRoot = Join-Path $env:APPDATA "TidyDock"
$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\TidyDock"

Get-Process -Name TidyDock -ErrorAction SilentlyContinue | Stop-Process -Force

if (Test-Path $runKey) {
    Remove-ItemProperty -Path $runKey -Name "TidyDock" -ErrorAction SilentlyContinue
}

if (Test-Path $uninstallKey) {
    Remove-Item -LiteralPath $uninstallKey -Recurse -Force
}

if (Test-Path $startMenuDir) {
    $resolvedStartMenu = Resolve-Path $startMenuDir
    $expectedStartMenuParent = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
    $resolvedStartMenuParent = Resolve-Path $expectedStartMenuParent
    if (-not $resolvedStartMenu.Path.StartsWith($resolvedStartMenuParent.Path, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove unexpected Start Menu path: $resolvedStartMenu"
    }
    Remove-Item -LiteralPath $startMenuDir -Recurse -Force
}

if (Test-Path $installRoot) {
    $resolvedInstall = Resolve-Path $installRoot
    $expectedInstallParent = Join-Path $env:LOCALAPPDATA "Programs"
    New-Item -ItemType Directory -Force -Path $expectedInstallParent | Out-Null
    $resolvedInstallParent = Resolve-Path $expectedInstallParent
    if (-not $resolvedInstall.Path.StartsWith($resolvedInstallParent.Path, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove unexpected install path: $resolvedInstall"
    }
    Remove-Item -LiteralPath $installRoot -Recurse -Force
}

if (-not $KeepData -and (Test-Path $dataRoot)) {
    $resolvedData = Resolve-Path $dataRoot
    $resolvedAppData = Resolve-Path $env:APPDATA
    if (-not $resolvedData.Path.StartsWith($resolvedAppData.Path, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove unexpected data path: $resolvedData"
    }
    Remove-Item -LiteralPath $dataRoot -Recurse -Force
}

Write-Host "TidyDock uninstalled."
if ($KeepData) {
    Write-Host "User data kept:"
    Write-Host $dataRoot
}
