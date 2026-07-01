# TidyDock Release Checklist

## Before Build

- Product boundary unchanged or documented.
- Version number updated where needed.
- Changelog updated.
- README updated.
- New settings have migration defaults.

## Build

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Build-Installer.ps1'
```

## Verification

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Test-Portable.ps1' -SkipLaunch
```

## Manual Smoke Test

- Launch portable build.
- Confirm single-instance behavior.
- Add and open each item type.
- Test folder panel.
- Test settings.
- Test tray.
- Test startup setting.
- Test install and uninstall.

## Performance Check

- Record idle private memory.
- Record idle working set.
- Record handle count.
- Record idle CPU.
- Compare against previous release.

## Release Artifacts

- `TidyDockSetup.exe`
- `TidyDock-portable.zip`
- Release notes.
- Known issues.

## Rollback

- Keep previous installer and portable package.
- Preserve config migration compatibility.
- Document any config version changes.


