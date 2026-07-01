# TidyDock User Flows

## First Launch

1. User starts TidyDock.
2. Dock appears using default settings.
3. Empty Dock shows a hint.
4. User opens context menu or settings.
5. User enables edit mode.
6. User adds first item.

## Add Item By Dragging

1. User enables edit mode.
2. User drags a file, folder, `.exe`, or `.lnk` onto the Dock.
3. TidyDock detects item type.
4. TidyDock inserts item at drop position or end.
5. TidyDock saves config.
6. Item appears immediately.

## Add URL

1. User enables edit mode.
2. User chooses Add URL.
3. User enters URL.
4. User enters display name.
5. TidyDock adds item and saves config.

## Open Item

1. User clicks a Dock item.
2. App/file/URL opens through the system handler.
3. Folder item opens folder panel instead.
4. Missing target shows warning and does not remove item.

## Reorder Item

1. User enables edit mode.
2. User drags Dock item.
3. Drag preview follows cursor center.
4. User drops before or after another item.
5. Manual order is saved.

## Use Folder Panel

1. User clicks folder item.
2. TidyDock asynchronously reads direct children.
3. Panel shows loading state.
4. Panel shows folders first, then files.
5. User clicks folder to enter or file to open.
6. User can go back, open in Explorer, close, or leave pointer to auto-hide.

## Change Settings

1. User opens settings.
2. User changes appearance or behavior.
3. Dock updates immediately.
4. Save is debounced.
5. Exit flushes pending save.

## Edge Cases

- Config file missing: create default config.
- Config file broken: copy broken file and recreate defaults.
- Target missing: warn user.
- Folder unreadable: show permission message.
- Tray disabled while Dock starts hidden: reject and explain.
- Multiple app launches: second instance exits.

