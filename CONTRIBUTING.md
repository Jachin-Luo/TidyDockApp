# Contributing To TidyDock

Thanks for helping improve TidyDock.

The project is intentionally small. Before adding features, check whether the idea fits the product boundary in [docs/00-vision.md](docs/00-vision.md).

## Development Setup

Use Windows and the .NET Framework MSBuild included with Windows:

```powershell
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe' `
  '.\src\TidyDock\TidyDock.csproj' `
  /p:Configuration=Release `
  /verbosity:minimal
```

Build release artifacts:

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Build-Installer.ps1'
```

Verify the portable package:

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Test-Portable.ps1' -SkipLaunch
```

## Pull Request Checklist

- Keep changes focused.
- Avoid unrelated refactors.
- Preserve the local-only privacy boundary.
- Update docs when behavior changes.
- Run a Release build.
- Run portable verification for packaging changes.
- Include before/after measurements for performance claims.

## Code Style

- Prefer existing project patterns.
- Keep WPF UI code practical and readable.
- Avoid background scanning, monitoring, indexing, or network behavior.
- Add comments only when they clarify non-obvious behavior.
- Keep strings localizable through `LocalizationService` when they appear in UI.

## Issue Guidelines

Good bug reports include:

- Windows version.
- TidyDock version or commit.
- Portable or installed build.
- Steps to reproduce.
- Expected behavior.
- Actual behavior.
- Relevant logs from `%APPDATA%\TidyDock\logs`.

Good feature requests include:

- The user problem.
- The core workflow.
- Why it fits TidyDock's manual lightweight scope.



