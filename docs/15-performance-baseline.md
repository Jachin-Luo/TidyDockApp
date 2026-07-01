# TidyDock Performance Baseline

Date: 2026-07-01

## Build Under Test

- Executable: `src/TidyDock/bin/Release/TidyDock.exe`
- File version: `0.1.1.0`
- Product version: `0.1.1.0`
- Executable size: 136704 bytes

## Command

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Measure-Memory.ps1' `
  -Launch `
  -StopAfter `
  -WarmupSeconds 30 `
  -Samples 8 `
  -IntervalSeconds 5 `
  -CsvPath '.\src\TidyDock\dist\memory-baseline.csv'
```

The script launched TidyDock, waited 30 seconds, collected 8 samples at 5-second intervals, saved CSV output, and stopped only the process it launched.

## Scenario

Baseline launch and idle measurement.

No manual interaction was performed during sampling.

## Results

### Baseline Before Optimization

| Metric | Average | Minimum | Maximum |
| --- | ---: | ---: | ---: |
| Working set | 6.75 MB | 4.64 MB | 7.51 MB |
| Private memory | 101.69 MB | 101.45 MB | 101.83 MB |
| Handles | 679.5 | 673 | 681 |
| Threads | 26.25 | 23 | 28 |
| CPU | 0% | 0% | 0% |

### After Icon Decode And Settings Window Lifetime Optimization

Command:

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock\scripts\Measure-Memory.ps1' `
  -Launch `
  -StopAfter `
  -WarmupSeconds 30 `
  -Samples 8 `
  -IntervalSeconds 5 `
  -CsvPath '.\src\TidyDock\dist\memory-after-icon-window-optimization.csv'
```

| Metric | Average | Minimum | Maximum |
| --- | ---: | ---: | ---: |
| Working set | 6.74 MB | 4.60 MB | 7.53 MB |
| Private memory | 72.29 MB | 72.02 MB | 72.45 MB |
| Handles | 679.38 | 673 | 681 |
| Threads | 26.25 | 23 | 28 |
| CPU | 0% | 0% | 0% |

Private memory decreased by about 29.41 MB, from 101.69 MB to 72.29 MB.

## Interpretation

Idle CPU is good in this baseline.

The working set is low because the app performs a startup working-set trim. This makes Task Manager-style visible memory look small, but private memory remains around 101 MB. For refactor work, private memory should be treated as the main memory metric, with working set recorded as a secondary user-visible metric.

The first optimization confirms that custom image icons were a major source of private memory usage. The app now decodes bitmap icons near their display size instead of loading full-resolution source images.

## Notes

- The baseline CSV is stored at `src/TidyDock/dist/memory-baseline.csv`.
- The optimized CSV is stored at `src/TidyDock/dist/memory-after-icon-window-optimization.csv`.
- `dist` is ignored by Git, so important summaries should be copied into this document.
- A quick attempt to parse the current user config with PowerShell JSON parsing failed, suggesting the local config may contain a malformed or encoding-damaged string. Config validation and recovery should be included in the first refactor phase.

## Next Measurements

- Fresh default config baseline.
- Current user config after explicit config validation.
- Open settings window, then idle.
- Open and close folder panel, then idle.
- Add 20-50 Dock items, then idle.
- 10-minute idle sample for resource growth.

