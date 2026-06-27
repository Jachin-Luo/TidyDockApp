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
- Hover magnification
- Borderless Dock surface with no icon hover background
- Dock background opacity can be set to 0 for a fully transparent Dock surface
- Optional Dock item name labels
- Folder stack panel with lazy directory read
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
- Tray icon visibility setting
- Language setting for core UI text, folder panel messages, and main dialogs
- Change Dock item icon from settings in edit mode
- Settings maintenance actions: open config folder, open log folder, clear icon cache, reset config, exit app
- Custom application and tray icon
- Portable build script
- Single-file current-user installer: `TidyDockSetup.exe`
- Current-user install/uninstall scripts
- Portable verification script
- Single-instance guard
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
