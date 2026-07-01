# TidyDock Security and Privacy

## Privacy Principle

TidyDock is local-first. MVP must not upload data, call remote APIs, or phone home.

## Data Stored

- User-selected Dock items.
- UI settings.
- Local icon cache.
- Local error log.

## Data Not Collected

- No desktop inventory.
- No process list.
- No window list.
- No recent files.
- No file contents.
- No analytics.
- No account data.

## File Safety

- Removing a Dock item must not delete the original file.
- Clearing icon cache must not affect original files.
- Folder panel reads direct child metadata only.
- No recursive scans.

## Permissions

- App should run as current user.
- Installer should not require administrator permission.
- Startup registration should use current-user registry only.

## Logs

- Logs stay in `%APPDATA%\TidyDock\logs`.
- Logs should avoid storing file contents or secrets.
- Unexpected exceptions may include paths; this is acceptable for local-only diagnostics.

