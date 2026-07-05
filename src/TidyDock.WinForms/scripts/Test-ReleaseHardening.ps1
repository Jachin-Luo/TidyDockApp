param(
    [string]$ExePath = "",
    [int]$IdleMinutes = 10,
    [int]$IdleIntervalSeconds = 30,
    [int]$FiftyItemSamples = 6,
    [int]$FiftyItemIntervalSeconds = 10
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$distRoot = Join-Path $projectRoot "dist"
if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $ExePath = Join-Path $distRoot "TidyDock-winforms-portable\TidyDock.exe"
}

$dataRoot = Join-Path $env:APPDATA "TidyDock\winforms"
$settingsPath = Join-Path $dataRoot "settings.json"
$preservedSettings = Join-Path ([System.IO.Path]::GetTempPath()) ("tidydock-settings-" + [guid]::NewGuid().ToString("N") + ".json")
$hadSettings = Test-Path -LiteralPath $settingsPath
$summaryPath = Join-Path $distRoot "release-hardening-summary.md"
$idleCsv = Join-Path $distRoot "release-hardening-10min-idle.csv"
$fiftyCsv = Join-Path $distRoot "release-hardening-50-items.csv"

function Stop-TidyDock {
    Get-Process -Name TidyDock,TidyDock.WinForms -ErrorAction SilentlyContinue | Stop-Process -Force
}

function Sample-Process {
    param(
        [System.Diagnostics.Process]$Process,
        [int]$Samples,
        [int]$IntervalSeconds
    )

    $rows = @()
    for ($i = 0; $i -lt $Samples; $i++) {
        $Process.Refresh()
        $rows += [pscustomobject]@{
            Timestamp = Get-Date -Format "o"
            ProcessId = $Process.Id
            WorkingSetMB = [math]::Round($Process.WorkingSet64 / 1MB, 2)
            PrivateMemoryMB = [math]::Round($Process.PrivateMemorySize64 / 1MB, 2)
            Handles = $Process.HandleCount
            CPUSeconds = [math]::Round($Process.TotalProcessorTime.TotalSeconds, 2)
        }
        if ($i -lt ($Samples - 1)) {
            Start-Sleep -Seconds $IntervalSeconds
        }
    }
    return $rows
}

function Write-FiftyItemConfig {
    New-Item -ItemType Directory -Force -Path $dataRoot | Out-Null
    $target = Join-Path $env:WINDIR "System32\notepad.exe"
    $items = @()
    for ($i = 1; $i -le 50; $i++) {
        $items += [ordered]@{
            Id = [guid]::NewGuid().ToString("N")
            Type = "app"
            Name = ("压测应用 {0:00}" -f $i)
            Target = $target
            IconPath = ""
            IconStyle = if ($i % 2 -eq 0) { "rounded-rect" } else { "default" }
            OpenBehavior = "auto"
            FolderGroups = @()
        }
    }

    $config = [ordered]@{
        ConfigVersion = 1
        Dock = [ordered]@{
            Position = "bottom"
            Display = "primary"
            IconSize = 52
            IconGap = 10
            Magnification = 1.42
            Opacity = 0.78
            CornerRadius = 20
            AutoHide = $false
            AlwaysOnTop = $false
            StartVisible = $true
            ShowTrayIcon = $true
            EnableDragReorder = $false
            WrapCustomIcons = $false
            HighContrastDock = $false
            Theme = "system"
            Language = "zh-CN"
        }
        FolderPanel = [ordered]@{
            MaxItems = 300
            MaxHeight = 520
            ShowHiddenFiles = $false
            ViewMode = "grid"
        }
        Items = $items
    }

    $config | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $settingsPath -Encoding UTF8
}

function Summarize-Rows {
    param([object[]]$Rows)

    $first = $Rows | Select-Object -First 1
    $last = $Rows | Select-Object -Last 1
    return [pscustomobject]@{
        Samples = $Rows.Count
        WorkingSetStartMB = $first.WorkingSetMB
        WorkingSetEndMB = $last.WorkingSetMB
        WorkingSetDeltaMB = [math]::Round($last.WorkingSetMB - $first.WorkingSetMB, 2)
        PrivateStartMB = $first.PrivateMemoryMB
        PrivateEndMB = $last.PrivateMemoryMB
        PrivateDeltaMB = [math]::Round($last.PrivateMemoryMB - $first.PrivateMemoryMB, 2)
        HandlesStart = $first.Handles
        HandlesEnd = $last.Handles
        HandlesDelta = $last.Handles - $first.Handles
        CpuDeltaSeconds = [math]::Round($last.CPUSeconds - $first.CPUSeconds, 4)
    }
}

if (-not (Test-Path -LiteralPath $ExePath)) {
    throw "Executable not found: $ExePath"
}

if ($hadSettings) {
    Copy-Item -LiteralPath $settingsPath -Destination $preservedSettings -Force
}

try {
    New-Item -ItemType Directory -Force -Path $distRoot | Out-Null
    Stop-TidyDock

    $idleSamples = [math]::Max(2, [math]::Ceiling(($IdleMinutes * 60) / $IdleIntervalSeconds) + 1)
    $idleProcess = Start-Process -FilePath $ExePath -PassThru
    Start-Sleep -Seconds 10
    $idleRows = Sample-Process -Process $idleProcess -Samples $idleSamples -IntervalSeconds $IdleIntervalSeconds
    $idleRows | Export-Csv -LiteralPath $idleCsv -NoTypeInformation -Encoding UTF8
    Stop-Process -Id $idleProcess.Id -Force

    Stop-TidyDock
    Write-FiftyItemConfig
    $fiftyProcess = Start-Process -FilePath $ExePath -PassThru
    Start-Sleep -Seconds 10
    $fiftyRows = Sample-Process -Process $fiftyProcess -Samples $FiftyItemSamples -IntervalSeconds $FiftyItemIntervalSeconds
    $fiftyRows | Export-Csv -LiteralPath $fiftyCsv -NoTypeInformation -Encoding UTF8
    Stop-Process -Id $fiftyProcess.Id -Force

    $idleSummary = Summarize-Rows -Rows $idleRows
    $fiftySummary = Summarize-Rows -Rows $fiftyRows
    @(
        "# TidyDock Release Hardening Summary",
        "",
        "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
        "",
        "## 10-minute idle",
        "",
        "| Metric | Value |",
        "| --- | ---: |",
        "| Samples | $($idleSummary.Samples) |",
        "| Private memory start | $($idleSummary.PrivateStartMB) MB |",
        "| Private memory end | $($idleSummary.PrivateEndMB) MB |",
        "| Private memory delta | $($idleSummary.PrivateDeltaMB) MB |",
        "| Working set delta | $($idleSummary.WorkingSetDeltaMB) MB |",
        "| Handles delta | $($idleSummary.HandlesDelta) |",
        "| CPU delta | $($idleSummary.CpuDeltaSeconds) s |",
        "",
        "CSV: ``$idleCsv``",
        "",
        "## 50-item Dock",
        "",
        "| Metric | Value |",
        "| --- | ---: |",
        "| Samples | $($fiftySummary.Samples) |",
        "| Private memory start | $($fiftySummary.PrivateStartMB) MB |",
        "| Private memory end | $($fiftySummary.PrivateEndMB) MB |",
        "| Private memory delta | $($fiftySummary.PrivateDeltaMB) MB |",
        "| Working set delta | $($fiftySummary.WorkingSetDeltaMB) MB |",
        "| Handles delta | $($fiftySummary.HandlesDelta) |",
        "| CPU delta | $($fiftySummary.CpuDeltaSeconds) s |",
        "",
        "CSV: ``$fiftyCsv``"
    ) | Set-Content -LiteralPath $summaryPath -Encoding UTF8

    Write-Host "Release hardening summary:"
    Write-Host $summaryPath
    Get-Content -LiteralPath $summaryPath
}
finally {
    Stop-TidyDock
    if ($hadSettings) {
        New-Item -ItemType Directory -Force -Path $dataRoot | Out-Null
        Copy-Item -LiteralPath $preservedSettings -Destination $settingsPath -Force
        Remove-Item -LiteralPath $preservedSettings -Force -ErrorAction SilentlyContinue
    }
    elseif (Test-Path -LiteralPath $settingsPath) {
        Remove-Item -LiteralPath $settingsPath -Force
    }
}
