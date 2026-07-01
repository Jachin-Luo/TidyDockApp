# TidyDock Documentation

This directory keeps product, design, development, testing, release, and planning notes for TidyDock.

## Recommended Reading Order

1. [00-vision.md](00-vision.md): product vision, target users, MVP boundary.
2. [01-prd.md](01-prd.md): product requirements and acceptance criteria.
3. [02-user-flows.md](02-user-flows.md): core workflows and edge cases.
4. [03-ui-spec.md](03-ui-spec.md): UI surfaces and interaction states.
5. [04-tech-design.md](04-tech-design.md): current architecture, refactor direction, technology choices.
6. [05-data-model.md](05-data-model.md): config, local storage, and migration rules.
7. [08-task-breakdown.md](08-task-breakdown.md): refactor task breakdown.
8. [09-test-plan.md](09-test-plan.md): test plan, including performance checks.
9. [10-release-checklist.md](10-release-checklist.md): release checklist.
10. [15-performance-baseline.md](15-performance-baseline.md): current performance baseline.
11. [adr/0001-tech-stack.md](adr/0001-tech-stack.md): architecture decision record.

## Current Refactor Principles

- Measure memory before changing the runtime or framework.
- Keep the MVP boundary small.
- Do not default to Electron, Tauri, WinUI, or Qt without data.
- Prefer clearer service boundaries inside the current Windows-native app first.
- Record before/after measurements for performance claims.

## Useful Commands

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Build-Installer.ps1'
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Test-Portable.ps1' -SkipLaunch
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Measure-Memory.ps1'
```

## Legacy Documents

The old Chinese-named requirement, design, and review files are retained as compatibility stubs. The current maintained documents are the numbered files listed above.
