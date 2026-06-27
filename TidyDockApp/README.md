# TidyDockApp

TidyDock is a lightweight Windows WPF desktop Dock built from the current requirement document, design document, and HTML prototype.

## Run

Development build:

```text
<workspace>\TidyDockApp\bin\Release\TidyDock.exe
```

Portable package:

```text
<workspace>\TidyDockApp\dist\TidyDock-portable\TidyDock.exe
```

Portable zip:

```text
<workspace>\TidyDockApp\dist\TidyDock-portable.zip
```

Installer package:

```text
<workspace>\TidyDockApp\dist\TidyDockSetup.exe
```

## Build

This machine does not have the .NET SDK installed, but it has .NET Framework MSBuild:

```powershell
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe' '<workspace>\TidyDockApp\TidyDockApp.csproj' /p:Configuration=Release /verbosity:minimal
```

Build portable output:

```powershell
powershell -ExecutionPolicy Bypass -File '<workspace>\TidyDockApp\scripts\Build-Portable.ps1'
```

Verify portable output:

```powershell
powershell -ExecutionPolicy Bypass -File '<workspace>\TidyDockApp\scripts\Test-Portable.ps1'
```

Build single-file installer:

```powershell
powershell -ExecutionPolicy Bypass -File '<workspace>\TidyDockApp\scripts\Build-Installer.ps1'
```

Install portable build for current user:

```powershell
powershell -ExecutionPolicy Bypass -File '<workspace>\TidyDockApp\dist\TidyDock-portable\Install-CurrentUser.ps1' -Launch
```

Uninstall current-user install:

```powershell
powershell -ExecutionPolicy Bypass -File "$env:LOCALAPPDATA\Programs\TidyDock\Uninstall-CurrentUser.ps1"
```

## Implemented

- Continuous Dock container with no default split zones
- Fresh default config starts with no sample Dock items
- App, shortcut, folder, file, URL, and optional separator items
- Edit mode gates Dock item editing
- Drag files, folders, `.exe`, and `.lnk` files into the Dock in edit mode
- Reorder Dock items by dragging in edit mode
- Hover magnification
- Borderless Dock surface with no icon hover background
- High-resolution Windows system icon extraction for sharper Dock icons
- Dock background opacity can be set to 0 for a fully transparent Dock surface
- Optional Dock item name labels
- Open apps, files, and URLs
- Open folder items as an on-demand scrollable folder panel
- Folder panel rows show compact file/folder icons
- Folder panel ignores stale async directory reads when users switch folders quickly
- Folder panel navigation: enter folder, go back, open in Explorer, auto-hide after pointer leaves
- `Esc` closes the folder panel and settings window
- Folder panel direction follows Dock position
- Default folder item limit: 300
- No recursive directory scan
- No thumbnail generation
- Right-click actions: open, show/open in Explorer, hide Dock; rename, edit target, change icon, add, remove, and reorder require edit mode
- Settings: display, position, icon size, gap, opacity, corner radius, magnification, auto-hide, always-on-top, startup, folder panel limits
- Startup visibility setting: choose whether the Dock is shown immediately on launch
- Always-on-top is off by default to avoid covering other applications
- Tray icon visibility setting with a safety guard
- Language setting for core UI text, folder panel messages, and main dialogs: `zh-CN` and `en-US`
- Input dialogs use the selected language for OK and Cancel buttons
- Theme setting: system, light, dark
- Dock item manager in settings
- Change Dock item icon from settings in edit mode
- Maintenance actions: open config folder, open log folder, clear icon cache, reset config, exit app
- Tray menu: show/hide, settings, about, exit
- Single-instance guard to prevent duplicate Dock processes
- Stable product-level single-instance mutex name
- Backup-assisted config replacement using a temporary file and `.bak` copy
- Debounced settings saves to reduce repeated disk writes during slider changes
- Startup idle working-set trim for lower resident memory
- Local error log for unexpected exceptions
- Local JSON config
- Local icon cache
- Assembly version info: `0.1.0.0`
- Custom application/tray icon
- Portable release script and zip output
- Single-file current-user installer: `TidyDockSetup.exe`
- Current-user install/uninstall scripts
- Portable verification script

## Local Data

Config:

```text
%APPDATA%\TidyDock\config\settings.json
```

Icon cache:

```text
%APPDATA%\TidyDock\cache\icons
```

Error log:

```text
%APPDATA%\TidyDock\logs\error.log
```

## Explicit Non-Goals

- No desktop scanning
- No automatic file organization
- No recent files
- No app running-state indicators
- No window list
- No task switching
- No process monitoring
- No background indexing
- No network behavior

## Pre-Release Checklist

- Run a Release installer build: `scripts\Build-Installer.ps1`
- Verify the portable package: `scripts\Test-Portable.ps1 -SkipLaunch`
- Smoke test install and uninstall with the current-user installer
- Confirm only one TidyDock process can run at once
- Confirm config recovery leaves `settings.json.bak` after an overwrite
- Test Dock positioning on the target display setup
- Scan README and release notes for mojibake or broken links

## Remaining Polish

- More refined control styling
- More detailed multi-monitor edge testing
- Signed installer package
