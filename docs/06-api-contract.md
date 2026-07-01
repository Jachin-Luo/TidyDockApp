# TidyDock API Contract

## External API

TidyDock has no backend API in the MVP. Network behavior is a non-goal.

## System Integration Contracts

### Shell Launch

Apps, files, folders, and URLs are opened through Windows shell behavior. Missing targets must show a warning.

### Explorer Integration

Folder items can open directly in Explorer. File items can be selected in Explorer when possible.

### Startup Registration

Start-with-Windows uses the current-user Run registry key:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

### Tray

The tray icon is optional, but must remain enabled when the Dock is configured to start hidden.

### Local Files

The app reads only user-selected paths and folder contents opened by the user. It must not recursively scan or index the filesystem.

## Internal Service Contracts

Future refactor services should use plain models and return explicit results instead of showing UI directly. UI layers decide how to display warnings.

