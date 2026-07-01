# TidyDock Dev Setup

## Requirements

- Windows.
- .NET Framework MSBuild at:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
```

## Build Release

```powershell
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe' `
  '.\src\TidyDock\TidyDock.csproj' `
  /p:Configuration=Release `
  /verbosity:minimal
```

## Build Portable Package

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Build-Portable.ps1'
```

Note: portable build stops running `TidyDock` processes before packaging.

## Build Installer

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Build-Installer.ps1'
```

## Verify Portable Package

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Test-Portable.ps1' -SkipLaunch
```

## Measure Runtime Memory

Sample an already running TidyDock process:

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Measure-Memory.ps1'
```

Launch, warm up, sample, save CSV, then stop only the process launched by the script:

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Measure-Memory.ps1' `
  -Launch `
  -StopAfter `
  -CsvPath '.\src\TidyDock\dist\memory-baseline.csv'
```

## Local Data

Development builds use the same data root as installed builds:

```text
%APPDATA%\TidyDock
```

Be careful when testing reset, uninstall, and config recovery.


