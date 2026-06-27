param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$distRoot = Join-Path $projectRoot "dist"
$portableRoot = Join-Path $distRoot "TidyDock-portable"
$setupPath = Join-Path $distRoot "TidyDockSetup.exe"
$sourcePath = Join-Path $projectRoot "installer\TidyDockSetup.cs"
$manifestPath = Join-Path $projectRoot "installer\TidyDockSetup.manifest"
$iconPath = Join-Path $projectRoot "assets\TidyDock.ico"
$buildPortable = Join-Path $PSScriptRoot "Build-Portable.ps1"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}

if (-not (Test-Path $sourcePath)) {
    throw "Installer source not found: $sourcePath"
}

if (-not (Test-Path $manifestPath)) {
    throw "Installer manifest not found: $manifestPath"
}

& powershell -ExecutionPolicy Bypass -File $buildPortable -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Portable build failed."
}

$payloadExe = Join-Path $portableRoot "TidyDock.exe"
$payloadIcon = Join-Path $portableRoot "TidyDock.ico"
$payloadReadme = Join-Path $portableRoot "README.txt"
$payloadUninstall = Join-Path $portableRoot "Uninstall-CurrentUser.ps1"

$payloads = @($payloadExe, $payloadIcon, $payloadReadme, $payloadUninstall)
foreach ($payload in $payloads) {
    if (-not (Test-Path $payload)) {
        throw "Missing installer payload: $payload"
    }
}

if (Test-Path $setupPath) {
    Remove-Item -LiteralPath $setupPath -Force
}

$args = @(
    "/nologo",
    "/target:winexe",
    "/platform:x86",
    "/optimize+",
    "/out:$setupPath",
    "/win32icon:$iconPath",
    "/win32manifest:$manifestPath",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/resource:$payloadExe,payload.TidyDock.exe",
    "/resource:$payloadIcon,payload.TidyDock.ico",
    "/resource:$payloadReadme,payload.README.txt",
    "/resource:$payloadUninstall,payload.Uninstall-CurrentUser.ps1",
    $sourcePath
)

& $csc @args
if ($LASTEXITCODE -ne 0) {
    throw "Installer build failed."
}

if (-not (Test-Path $setupPath)) {
    throw "Installer output not found: $setupPath"
}

Write-Host "Installer package:"
Write-Host $setupPath
