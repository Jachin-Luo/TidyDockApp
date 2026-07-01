# TidyDock Data Model

## Storage Root

```text
%APPDATA%\TidyDock\
  config\settings.json
  config\settings.json.bak
  cache\icons\
  shortcuts\
  logs\error.log
```

## Config Shape

Current config version: `4`.

Top-level fields:

- `Version`
- `Dock`
- `FolderPanel`
- `Items`

## Dock Settings

- `Position`
- `Display`
- `IconSize`
- `IconGap`
- `Magnification`
- `Opacity`
- `CornerRadius`
- `AutoHide`
- `AlwaysOnTop`
- `StartWithWindows`
- `StartVisible`
- `ShowTrayIcon`
- `ShowItemLabels`
- `EditMode`
- `Language`
- `Theme`

## Folder Panel Settings

- `MaxItems`
- `ShowHiddenFiles`
- `MaxHeight`

## Dock Item

- `Id`: stable unique id.
- `Type`: `app`, `folder`, `file`, `url`, `separator`, or internal item type.
- `Name`: display name.
- `Target`: path or URL.
- `Icon`: optional custom icon path.

For `.lnk` shortcuts, TidyDock imports a copy into `%APPDATA%\TidyDock\shortcuts` and stores the copied shortcut path as `Target`. This prevents Dock items from breaking when the original desktop shortcut is deleted.

## Migration Rules

- Never delete user files during migration.
- Preserve unknown values where possible.
- Add defaults for missing settings.
- Keep version bump explicit.
- Keep a backup before replacing config.
- Existing `.lnk` app items are migrated to the internal shortcut directory when the original shortcut still exists.

## Refactor Notes

If a new architecture changes item shape, add a config version rather than silently changing semantics.
