# TidyDock WinForms

This is the v0.2 Windows-only rewrite focused on lower resident resource usage.

The previous WPF project remains in `src/TidyDock` as the v0.1 legacy implementation and reference.

## Goals

- Keep TidyDock as a small manual Dock, not a taskbar replacement.
- Use WinForms plus Win32 interop instead of WPF control trees for the always-on Dock.
- Store new v0.2 config separately from the v0.1 WPF config.
- Keep packaging local and current-user friendly.

## Local Data

```text
%APPDATA%\TidyDock\winforms\
  settings.json
  settings.json.bak
  cache\
  shortcuts\
  logs\tidydock.log
```

The v0.2 config does not migrate or overwrite the WPF v0.1 config.

## Build

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock.WinForms\scripts\Build-Installer.ps1'
```

Outputs:

```text
src\TidyDock.WinForms\dist\TidyDockWinFormsSetup.exe
src\TidyDock.WinForms\dist\TidyDock-winforms-portable.zip
src\TidyDock.WinForms\dist\TidyDock-winforms-portable\
```

## Verify

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock.WinForms\scripts\Test-Portable.ps1'
```

## Current MVP

- Borderless self-drawn Dock window.
- Glass-style Dock base and hover magnification.
- App, file, folder, URL, separator, and settings items.
- Drag files/folders into the Dock.
- Drag Dock items to reorder.
- Right-click Dock menu.
- Settings window with explicit dark/light colors.
- Tray menu.
- Auto-hide hot zone.
- Current-user startup setting.
- Portable zip and current-user installer.
