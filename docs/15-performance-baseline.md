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

### WinForms v0.2 Preview

Command:

```powershell
powershell -ExecutionPolicy Bypass -File '.\src\TidyDock.WinForms\scripts\Measure-Memory.ps1' `
  -Launch `
  -StopAfter `
  -WarmupSeconds 15 `
  -Samples 4 `
  -IntervalSeconds 3
```

| Metric | Average | Minimum | Maximum |
| --- | ---: | ---: | ---: |
| Working set | 48.34 MB | 48.34 MB | 48.34 MB |
| Private memory | 9.95 MB | 9.92 MB | 9.96 MB |
| Handles | 314 | 314 | 314 |
| CPU seconds | 0.28 | 0.28 | 0.28 |

The WinForms rewrite preview reduces idle private memory by about 62.34 MB compared with the optimized WPF v0.1.1 measurement.

After the first WinForms UI polish pass, a short 4-sample check measured about 18.53 MB idle private memory. The visual polish did not materially change the lower-resource direction.

After the transparent glass UI and Chinese localization pass, a short 4-sample check measured about 18.54 MB idle private memory, with stable CPU seconds during the sample. The added DWM backdrop and lightweight custom controls did not materially increase idle private memory.

After adding the WinForms folder Flyout and folder panel settings, a short 4-sample idle check measured about 18.87 MB private memory, with stable CPU seconds during the sample. The folder service remains lazy and does not scan folders until a folder Dock item is clicked.

### WinForms v0.2 发布硬化短测 - 2026-07-05

本次样本来自 self-contained portable 安装并启动后的 5 秒 idle 检查：

```powershell
$p1 = Get-Process TidyDock -ErrorAction SilentlyContinue | Select-Object -First 1
Start-Sleep -Seconds 5
$p2 = Get-Process -Id $p1.Id
[pscustomobject]@{
  CpuDeltaSeconds = [math]::Round(($p2.CPU - $p1.CPU), 4)
  WorkingSetMB = [math]::Round($p2.WorkingSet64 / 1MB, 2)
  PrivateMemoryMB = [math]::Round($p2.PrivateMemorySize64 / 1MB, 2)
  Handles = $p2.HandleCount
} | Format-List
```

| Metric | Value |
| --- | ---: |
| CPU delta over 5s | 0 s |
| Working set | 66.45 MB |
| Private memory | 16.67 MB |
| Handles | 481 |

Self-contained 发布主要增加包体积，不明显增加空闲 private memory。本次产物大小：

| Artifact | Size |
| --- | ---: |
| `TidyDock.exe` | 146.51 MB |
| `TidyDock-winforms-portable.zip` | 59.10 MB |
| `TidyDockWinFormsSetup.exe` | 146.62 MB |

### WinForms v0.2 发布硬化长测 - 2026-07-05

命令：

```powershell
.\src\TidyDock.WinForms\scripts\Test-ReleaseHardening.ps1
```

输出摘要：

```text
src\TidyDock.WinForms\dist\release-hardening-summary.md
src\TidyDock.WinForms\dist\release-hardening-10min-idle.csv
src\TidyDock.WinForms\dist\release-hardening-50-items.csv
```

10 分钟 idle：

| Metric | Value |
| --- | ---: |
| Samples | 21 |
| Private memory start | 16.05 MB |
| Private memory end | 15.89 MB |
| Private memory delta | -0.16 MB |
| Working set delta | 0.26 MB |
| Handles delta | -5 |
| CPU delta | 0 s |

50 项 Dock：

| Metric | Value |
| --- | ---: |
| Samples | 6 |
| Private memory start | 16.37 MB |
| Private memory end | 16.28 MB |
| Private memory delta | -0.09 MB |
| Working set delta | -0.05 MB |
| Handles delta | 2 |
| CPU delta | 0 s |

结论：本轮键盘可达性、菜单主题、设置页按钮收纳和压测脚本没有引入空闲 CPU 或私有内存增长。

## Interpretation

Idle CPU is good in this baseline.

The working set is low because the app performs a startup working-set trim. This makes Task Manager-style visible memory look small, but private memory remains around 101 MB. For refactor work, private memory should be treated as the main memory metric, with working set recorded as a secondary user-visible metric.

The first optimization confirms that custom image icons were a major source of private memory usage. The app now decodes bitmap icons near their display size instead of loading full-resolution source images.

## Notes

- The baseline CSV is stored at `src/TidyDock/dist/memory-baseline.csv`.
- The optimized CSV is stored at `src/TidyDock/dist/memory-after-icon-window-optimization.csv`.
- The WinForms preview CSV is stored at `src/TidyDock.WinForms/dist/memory-winforms.csv`.
- `dist` is ignored by Git, so important summaries should be copied into this document.
- Config validation and recovery now preserve broken `settings.json` files, attempt automatic backup recovery, and fall back to defaults only when recovery fails.

## Next Measurements

- Fresh default config baseline.
- Current user config after explicit config validation.
- Open settings window, then idle.
- Open and close folder panel, then idle.
- Add 20-50 Dock items, then idle.
- 10-minute idle sample for resource growth.

