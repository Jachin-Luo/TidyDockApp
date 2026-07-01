# TidyDock Technical Design

## Current State

TidyDock is a .NET Framework 4.6.1 WPF application. Most UI is built directly in code. The largest classes are:

- `MainWindow.cs`: Dock rendering, menus, drag/drop, item operations, positioning, auto-hide.
- `SettingsWindow.cs`: settings UI and item manager.
- `FolderPanel.cs`: folder popup and async directory reads.

This works, but future changes will become risky because product logic, UI rendering, persistence, and shell integration are tightly coupled.

## Refactor Direction

The first refactor should be architecture-first, not framework-first.

Recommended module boundaries:

- `DockItemService`: add, remove, rename, edit target, move, infer item type.
- `DockLayoutService`: screen selection, Dock bounds, hot-zone bounds, orientation.
- `DockMenuBuilder`: Dock and item context menus.
- `DockRenderer`: build Dock item controls from view state.
- `FolderPanelService`: read folder entries with limits and sorting.
- `ConfigMigrationService`: config version upgrades.
- `PerformanceTelemetry`: local-only startup and memory snapshots for development builds.

## Memory Reduction Hypotheses

Potential contributors to high runtime memory:

- WPF framework base cost.
- Shell icon extraction and cached bitmap objects.
- Re-rendering the full Dock after every setting change.
- Creating settings/folder UI eagerly or retaining heavy controls.
- Drop shadow and transparent windows.
- Running on .NET Framework instead of a newer runtime.

Each hypothesis needs measurement. Do not assume a framework rewrite will reduce memory.

## Technology Options

### Stay on WPF and Refactor

Best first step for Windows-native MVP. It preserves existing behavior and lets us measure real improvements from lazy loading, smaller object lifetimes, and clearer services.

### Upgrade to Modern .NET WPF

Worth evaluating after the architecture refactor. It may improve runtime behavior and tooling, but packaging and target-machine requirements must be checked.

### Tauri

Good for lightweight cross-platform desktop apps, but it uses WebView2 on Windows. It is not automatically lower-memory for a tiny always-on-top Dock.

### Electron

Fast development and strong web ecosystem, but generally a poor fit if idle memory is the main concern.

### WinUI

Native-looking Windows UI, but Windows App SDK overhead and packaging complexity need measurement.

### Qt

Good for complex cross-platform GUI. It is likely too much migration cost for the current MVP unless cross-platform becomes a hard requirement.

## Recommended Decision

For the next iteration, keep the product Windows-native and refactor the current WPF architecture first. Revisit runtime migration only after we have baseline and post-refactor memory data.

## Verification

- Build Release.
- Run portable verification.
- Measure private memory, working set, managed heap, handle count, and idle CPU.
- Smoke test Dock item workflows, folder panel, settings, tray, startup, and install/uninstall.

