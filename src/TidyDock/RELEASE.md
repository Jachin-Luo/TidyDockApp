# TidyDock Release Notes

## 0.1.0

First usable Windows WPF build based on the current requirement document, design document, and HTML prototype.

Included:

- Continuous Dock container with no default split zones
- Fresh default config starts with no sample Dock items
- Manual app, shortcut, folder, file, URL, and separator items
- Edit mode gates Dock item editing
- Drag to add items in edit mode
- Drag to reorder Dock items in edit mode
- Cursor-centered Dock drag preview for clearer item repositioning
- Hover magnification
- Borderless Dock surface with no icon hover background
- High-resolution Windows system icon extraction for sharper Dock icons
- Dock background opacity can be set to 0 for a fully transparent Dock surface
- Optional Dock item name labels
- Folder stack panel with lazy directory read
- Compact rounded folder panel layout with a lighter translucent surface
- Folder panel rows show compact file/folder icons
- Folder panel hides `.lnk` suffixes in display names
- Folder panel guards against stale async reads while switching folders quickly
- Folder navigation, back, Explorer open, and pointer-leave auto-hide
- Esc closes folder panel and settings window
- Local JSON configuration
- Local icon cache
- Tray menu
- Startup option
- Theme option: system, light, dark
- Display selection
- Startup visibility setting
- Always-on-top is off by default
- Dock restores itself after Windows Show Desktop minimizes it
- Tray icon visibility setting
- Language setting for core UI text, folder panel messages, and main dialogs
- Localized input dialog OK and Cancel buttons
- Change Dock item icon from settings in edit mode
- Settings maintenance actions: open config folder, open log folder, clear icon cache, reset config, exit app
- Custom application and tray icon
- Portable build script
- Single-file current-user installer: `TidyDockSetup.exe`
- Installer manifest declares current-user `asInvoker` execution to avoid Windows Program Compatibility Assistant false positives
- Current-user install/uninstall scripts
- Portable verification script
- Single-instance guard
- Stable product-level single-instance mutex name
- Backup-assisted atomic config save with `.bak`
- Debounced settings saves during frequent UI changes
- Startup idle working-set trim for lower resident memory
- Local error log for unexpected exceptions

Explicitly excluded:

- Desktop scanning
- Automatic file organization
- Recent files
- App running-state indicators
- Window list
- Task switching
- Process monitoring
- Background indexing
- Network behavior
