param(
    [switch]$RemoveUserData
)

$ErrorActionPreference = "Stop"

Get-Process -Name TidyDock -ErrorAction SilentlyContinue | Stop-Process -Force

$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
Remove-ItemProperty -Path $runKey -Name "TidyDock.WinForms" -ErrorAction SilentlyContinue

$target = Join-Path $env:LOCALAPPDATA "Programs\TidyDock.WinForms"
if (Test-Path $target) {
    Remove-Item -LiteralPath $target -Recurse -Force
}

$startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\TidyDock"
if (Test-Path $startMenu) {
    Remove-Item -LiteralPath $startMenu -Recurse -Force
}

if ($RemoveUserData) {
    $userData = Join-Path $env:APPDATA "TidyDock\winforms"
    if (Test-Path $userData) {
        $resolvedUserData = (Resolve-Path -LiteralPath $userData).Path
        $resolvedAppData = (Resolve-Path -LiteralPath $env:APPDATA).Path.TrimEnd('\') + '\'
        if (-not $resolvedUserData.StartsWith($resolvedAppData, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove user data outside APPDATA: $resolvedUserData"
        }
        Remove-Item -LiteralPath $resolvedUserData -Recurse -Force
    }
    Write-Host "TidyDock WinForms removed for current user. User configuration was also removed."
} else {
    Write-Host "TidyDock WinForms removed for current user. User configuration was kept in %APPDATA%\TidyDock\winforms."
}
