# UI Regression Tests

Windows-only test harness using the production WinForms controls, without extra test packages.

```powershell
dotnet run --project tests/TidyDock.UiTests/TidyDock.UiTests.csproj -c Release -- src/TidyDock.WinForms/dist/ui-review
```

Use `--system-dpi` to also exercise the current Windows DPI setting:

```powershell
dotnet run --project tests/TidyDock.UiTests/TidyDock.UiTests.csproj -c Release -- src/TidyDock.WinForms/dist/ui-review-system-dpi --system-dpi
```

Coverage:

- Keyboard, focus loss, disabled states, accessibility actions and mouse capture for shared controls.
- Filtered item movement/removal, repeated undo, empty results and keyboard category navigation.
- Draft preview/save state, theme rebuild disposal, preserved selection and magnification limits.
- All five settings categories in light/dark themes at 860, 960 and 1280 pixel window widths.
- Horizontal overflow, sibling overlap, bounded lists, visible action rows and accent text contrast.

The harness constructs offscreen windows with synthetic items. Preview and commit callbacks are in-memory only; it never loads or saves the user's configuration or changes startup registration. Rendering uses `DrawToBitmap`; generated PNGs are diagnostic renders, not desktop captures. Mixed-monitor DPI transitions and actual assistive technology still need manual verification.

## Compositor Verification

```powershell
dotnet run --project tests/TidyDock.UiTests/TidyDock.UiTests.csproj -c Release -- src/TidyDock.WinForms/dist/frost-review --glass-preview --system-dpi
```

This optional test briefly shows a generated striped scene, the material sample, settings, and production Dock/flyout windows. It checks blur attenuation, retained backdrop color, rounded input regions, and material cleanup after hiding. Screenshots are restricted to the test scene; a marker check rejects captures when the scene is obscured.

Dock/flyout fixtures use an explicit data root inside the output directory. Only generated documents and configuration are written there. The user's Dock data and startup registration are untouched. The test extends the flyout's close delay only in its fixture to permit visual inspection.
