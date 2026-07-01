# TidyDock Product Vision

## Product Definition

TidyDock is a lightweight Windows desktop Dock for users who want a clean, manual launcher for frequently used apps, folders, files, and URLs.

It is not a taskbar replacement, desktop scanner, file organizer, recent-file tool, process monitor, or automation system.

## Target Users

- Windows users who prefer a macOS-like Dock launcher.
- Users who manually curate their workspace and do not want automatic desktop scanning.
- Users who care about low idle resource usage and local-only data.

## Core Problem

Windows users often keep frequently used entry points across taskbar pins, desktop shortcuts, Start menu, Explorer favorites, and browser bookmarks. TidyDock gives them one small, manually organized launch surface without indexing or monitoring the system.

## MVP Goal

The MVP should prove one complete loop:

1. User launches TidyDock.
2. User enables edit mode.
3. User adds apps, folders, files, URLs, and separators.
4. User reorders and edits items.
5. User opens items quickly.
6. User configures appearance and behavior.
7. User exits and restarts with configuration preserved.

## Non-Goals

- No desktop scan.
- No automatic classification or cleanup.
- No recent files.
- No running-state indicators.
- No task switching.
- No window management.
- No background indexing.
- No cloud sync.
- No network behavior in MVP.

## Refactor Objective

The first refactor should reduce complexity and measured runtime memory without expanding product scope. The priority is a smaller, more maintainable Windows-native MVP, not a broad feature rewrite.

## Success Criteria

- Idle CPU stays near 0%.
- Dock can stay open all day without visible memory growth.
- Folder contents are only read when clicked.
- Configuration writes are debounced and durable.
- Core user flow remains usable after install, portable launch, and restart.

