# TidyDock PRD

## Summary

TidyDock is a Windows desktop Dock for manually organized launch items. It should feel fast, quiet, local, and predictable.

## MVP Scope

### Dock Items

- Add app, shortcut, folder, file, URL, and separator items.
- Rename items.
- Edit item target.
- Change item icon.
- Remove item from Dock without deleting original files.
- Reorder items manually.

### Dock Behavior

- Position Dock at bottom, top, left, or right edge.
- Select display.
- Hover magnification.
- Optional item labels.
- Optional transparent Dock background.
- Auto-hide with edge hot zone.
- Always-on-top toggle.
- Show or hide tray icon with safety guard.
- Start visible toggle.
- Start with Windows toggle.

### Folder Panel

- Show direct children only.
- Do not recurse.
- Do not read file contents.
- Do not generate thumbnails.
- Sort folders before files.
- Hide `.lnk` suffix in display text only.
- Limit visible items and show overflow hint.
- Open files through default system handler.
- Navigate into subfolders and back.
- Open current folder in Explorer.

### Local Data

- Store settings in `%APPDATA%\TidyDock\config\settings.json`.
- Store icon cache in `%APPDATA%\TidyDock\cache\icons`.
- Store logs in `%APPDATA%\TidyDock\logs`.

## Refactor Requirements

- Preserve existing user configuration where possible.
- Keep config migration explicit and versioned.
- Keep UI behavior functionally equivalent unless intentionally changed.
- Introduce module boundaries before broad UI redesign.
- Add performance measurement before claiming memory improvement.

## Performance Targets

These targets need baseline measurement before finalizing:

- Idle CPU: near 0%.
- Private memory after warm idle: target TBD after baseline.
- Working set after warm idle: target TBD after baseline.
- No unbounded icon or folder-entry cache growth.
- Folder panel should remain responsive with large folders within configured item limit.

## Acceptance Criteria

- Release build succeeds.
- Portable package verification succeeds.
- Existing manual Dock workflows still work.
- Settings persist across restart.
- Broken config recovery still works.
- Single-instance guard still works.
- Measured memory is documented before and after refactor.

