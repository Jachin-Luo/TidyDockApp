# TidyDock Refactor Task Breakdown

See also:

- [v0.2.0 development plan](plans/v0.2.0.md)
- [v0.2.0 issue backlog](issues/v0.2.0-backlog.md)

## Phase 0: Baseline

- Record current product boundary.
- Record current user flows.
- Measure current memory, handle count, idle CPU, and startup time.
- Use `src/TidyDock/scripts/Measure-Memory.ps1` for repeatable memory samples.
- Identify whether memory issue is working set, private bytes, managed heap, icon cache, or window resources.

## Phase 1: Architecture Separation

- Add config validation and repair notes for malformed local config cases.
- Extract Dock item operations into `DockItemService`.
- Extract screen and hot-zone positioning into `DockLayoutService`.
- Extract folder entry reading into a testable service.
- Keep UI behavior unchanged.

## Phase 2: Rendering and Lifetime

- Reduce full Dock re-rendering where possible.
- Lazy-create settings window and release it on close.
- Lazy-create folder panel and clear content on close.
- Audit icon image lifetime and cache behavior.
- Avoid retaining stale UI controls.

## Phase 3: Config and Migration

- Make config normalization and migration explicit.
- Add tests or scriptable checks for broken config recovery.
- Keep `.bak` replacement behavior.

## Phase 4: Performance Pass

- Keep repeatable memory measurement script current.
- Compare baseline vs refactor.
- Check idle CPU.
- Check large folder behavior.
- Check icon cache size and loading behavior.

## Phase 5: Release Hardening

- Run Release build.
- Run portable verification.
- Smoke test installer and uninstaller.
- Update changelog.
- Update release checklist.

