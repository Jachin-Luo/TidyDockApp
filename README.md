# TidyDock

TidyDock is a lightweight Windows desktop Dock inspired by the macOS Dock. It focuses on manual organization, low resource usage, and a clean desktop launcher experience.

## Status

Phase 1 is usable as a WPF/.NET Framework desktop application with a portable package and a single-file current-user installer.

## Features

- Manual Dock items: apps, shortcuts, folders, files, URLs, and separators
- No desktop scanning, app monitoring, recent files, or background indexing
- Dock item order stays manual and does not change based on recent tasks
- Edit mode gates item editing, drag-to-add, drag reorder, rename, icon change, and removal
- Optional icon name labels
- Fully transparent Dock background option
- Folder stack popup with lazy directory reads, file type icons, and quick auto-hide
- Local JSON config with backup-assisted atomic replacement
- Local icon cache
- Stable single-instance guard
- Debounced settings saves for smoother slider changes
- Tray menu, startup option, portable package, and current-user installer

## Repository Structure

```text
.
|-- TidyDockApp/          WPF application source, scripts, installer source
|-- docs/                 Requirement/design documents and HTML prototype
|-- .gitignore
`-- README.md
```

## Build

This project targets .NET Framework 4.6.1 and builds with the .NET Framework MSBuild included on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File ".\TidyDockApp\scripts\Build-Installer.ps1"
```

The build script generates:

```text
TidyDockApp\dist\TidyDockSetup.exe
TidyDockApp\dist\TidyDock-portable.zip
```

Build outputs are intentionally ignored by Git.

## Verify

```powershell
powershell -ExecutionPolicy Bypass -File ".\TidyDockApp\scripts\Test-Portable.ps1" -SkipLaunch
```

## Documentation

- Requirement document: `docs/TidyDock_需求文档.md`
- Design document: `docs/TidyDock_设计文档.md`
- Review notes: `docs/TidyDock_项目审视优化建议_20260627.md`
- HTML prototype: `docs/TidyDock_原型.html`
- App README: `TidyDockApp/README.md`
- Release notes: `TidyDockApp/RELEASE.md`

## Product Boundary

TidyDock is not a taskbar replacement, file organizer, process monitor, or automation tool. It is a lightweight manual Dock for launching and organizing user-selected entries.
