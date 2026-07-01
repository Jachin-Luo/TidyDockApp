# TidyDock Test Plan

## Build Tests

- Release build succeeds.
- Portable package builds.
- Installer builds.
- Portable verification passes.

## Functional Smoke Tests

- Launch app.
- Single-instance guard works.
- Enable and disable edit mode.
- Add app/file/folder/URL/separator.
- Reorder item.
- Rename item.
- Edit target.
- Change icon.
- Remove item without deleting source file.
- Open items.
- Open settings.
- Toggle tray icon with safety guard.
- Toggle start-with-Windows.

## Folder Panel Tests

- Open folder.
- Enter subfolder.
- Go back.
- Open in Explorer.
- Show too-many-items hint.
- Hidden files follow setting.
- Permission errors display safely.
- Quick folder switching does not show stale results.

## Config Tests

- Fresh config created.
- Settings persist after restart.
- Broken config is preserved and replaced by defaults.
- Save creates or updates `.bak`.
- Exit flushes pending save.

## Performance Tests

- Use `src/TidyDock/scripts/Measure-Memory.ps1` for repeatable samples.
- Measure cold launch memory.
- Measure warm idle memory after 30s.
- Measure memory after opening settings.
- Measure memory after opening and closing folder panel.
- Measure memory after adding 50 Dock items.
- Measure idle CPU.
- Measure handle count.

## Install Tests

- Current-user install.
- Launch from Start Menu.
- Uninstall with data removal.
- Uninstall with data kept.
- No administrator permission required.

