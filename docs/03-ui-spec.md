# TidyDock UI Spec

## Primary Surfaces

- Dock window.
- Dock context menu.
- Item context menu.
- Folder panel popup.
- Settings window.
- Input dialogs.
- Tray menu.

## Dock Window

- Borderless transparent WPF window.
- Contains one continuous item row or column.
- Orientation follows Dock position.
- Empty state uses one compact hint button.
- Dock background opacity can be 0.
- Optional item labels use trimmed text.
- Separator is a visual divider only.

## Dock Item States

- Normal.
- Hover magnified.
- Drag source dimmed.
- Missing target warning on activation.
- Edit actions disabled when edit mode is off.

## Context Menus

Item menu:

- Open.
- Rename, edit target, change icon when edit mode is on.
- Show or open in Explorer.
- Remove from Dock when edit mode is on.
- Shared Dock actions.

Dock menu:

- Edit mode toggle.
- Add URL.
- Add separator.
- Settings.
- Hide Dock.
- About.

## Folder Panel

- Compact popup anchored to Dock item.
- Placement follows Dock position.
- Header contains back, title, Explorer, close.
- Rows show icon and trimmed display name.
- Hidden files follow setting.
- Overflow shows hint instead of loading more.

## Settings Window

Sections:

- Appearance.
- Behavior.
- Dock items.
- Maintenance.

Current implementation keeps a simple white settings window. A later UI pass may align it with `ThemeService`, but the refactor should not block on visual redesign.

## Dialogs

- Input dialogs should localize OK and Cancel.
- Escape should close settings and folder panel.

