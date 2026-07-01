# ADR 0001: Refactor Before Framework Migration

## Status

Proposed

## Context

TidyDock currently uses .NET Framework 4.6.1 and WPF. The app is functional, but the architecture is concentrated in a few large classes and runtime memory is considered too high.

Several alternatives are possible:

- Continue WPF and refactor architecture.
- Upgrade to modern .NET WPF.
- Rewrite with Tauri.
- Rewrite with Electron.
- Rewrite with WinUI.
- Rewrite with Qt.

## Decision

For the next iteration, keep the Windows-native direction and refactor the current WPF architecture before committing to a framework migration.

The refactor should first measure memory, reduce retained UI/resources, separate services from UI, and preserve existing behavior.

## Rationale

- The product is Windows-first today.
- Electron is unlikely to help memory.
- Tauri uses WebView2 and is not guaranteed to beat optimized WPF for an always-on-top Dock.
- WinUI and modern .NET may help later, but migration cost should be justified by measurements.
- Architecture separation is valuable regardless of future technology.

## Consequences

- Short-term work focuses on maintainability and measurement.
- Framework migration remains open, but blocked on baseline data.
- Refactor should avoid feature expansion until memory and structure improve.

