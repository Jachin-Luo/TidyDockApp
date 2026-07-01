# Changelog

## Unreleased

- Prepare the `0.2.0` development plan and issue backlog.

## 0.1.1 - 2026-07-01

- Added process documentation for product vision, PRD, user flows, technical design, data model, tests, release, privacy, and refactor planning.
- Documented refactor direction focused on measurable memory reduction before framework migration.
- Added first performance baseline summary and repeatable memory measurement script.
- Reduced idle private memory from about 101.69 MB to 72.29 MB by decoding custom bitmap icons near display size and avoiding duplicate startup rendering.
- Hid the Dock and auto-hide hot zone from the Alt+Tab application switcher.
- Added delayed restore after Windows Show Desktop minimizes the Dock.
- Imported `.lnk` shortcuts into the local TidyDock data directory so Dock items do not depend on desktop shortcut files.
- First usable Windows WPF build.
- Manual Dock items.
- Folder panel.
- Settings window.
- Tray menu.
- Portable package.
- Current-user installer.
- Local config, icon cache, and error log.

## 0.1.0

- Early preview tag before the repository restructuring and release documentation pass.
