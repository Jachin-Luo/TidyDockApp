param(
    [string]$ExePath = "",
    [int]$WarmupSeconds = 10,
    [int]$Samples = 6,
    [int]$IntervalSeconds = 5,
    [switch]$Launch,
    [switch]$StopAfter,
    [string]$CsvPath = ""
)

$ErrorActionPreference = "Stop"

if ($Samples -lt 1) {
    throw "Samples must be at least 1."
}

if ($IntervalSeconds -lt 1) {
    throw "IntervalSeconds must be at least 1."
}

$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $releaseExe = Join-Path $projectRoot "bin\Release\TidyDock.exe"
    $portableExe = Join-Path $projectRoot "dist\TidyDock-portable\TidyDock.exe"
    if (Test-Path $releaseExe) {
        $ExePath = $releaseExe
    }
    elseif (Test-Path $portableExe) {
        $ExePath = $portableExe
    }
}

$startedProcess = $null
$process = $null

if ($Launch) {
    if ([string]::IsNullOrWhiteSpace($ExePath) -or -not (Test-Path $ExePath)) {
        throw "TidyDock.exe not found. Build Release or pass -ExePath."
    }

    $startedProcess = Start-Process -FilePath $ExePath -PassThru
    Start-Sleep -Seconds $WarmupSeconds
    $process = Get-Process -Id $startedProcess.Id -ErrorAction SilentlyContinue
}
else {
    $process = Get-Process -Name TidyDock -ErrorAction SilentlyContinue |
        Sort-Object StartTime |
        Select-Object -First 1
}

if ($null -eq $process) {
    throw "No running TidyDock process found. Start TidyDock or rerun with -Launch."
}

$processorCount = [Environment]::ProcessorCount
$previousCpu = $null
$previousTimestamp = $null
$results = New-Object System.Collections.Generic.List[object]

for ($i = 0; $i -lt $Samples; $i++) {
    $process.Refresh()
    $timestamp = Get-Date
    $cpuSeconds = $process.TotalProcessorTime.TotalSeconds
    $cpuPercent = $null

    if ($previousCpu -ne $null -and $previousTimestamp -ne $null) {
        $elapsed = ($timestamp - $previousTimestamp).TotalSeconds
        if ($elapsed -gt 0) {
            $cpuPercent = (($cpuSeconds - $previousCpu) / $elapsed / $processorCount) * 100
        }
    }

    $results.Add([pscustomobject]@{
        Timestamp = $timestamp.ToString("yyyy-MM-dd HH:mm:ss")
        ProcessId = $process.Id
        WorkingSetMB = [math]::Round($process.WorkingSet64 / 1MB, 2)
        PrivateMemoryMB = [math]::Round($process.PrivateMemorySize64 / 1MB, 2)
        PagedMemoryMB = [math]::Round($process.PagedMemorySize64 / 1MB, 2)
        VirtualMemoryMB = [math]::Round($process.VirtualMemorySize64 / 1MB, 2)
        Handles = $process.HandleCount
        Threads = $process.Threads.Count
        CpuPercent = if ($cpuPercent -eq $null) { $null } else { [math]::Round($cpuPercent, 2) }
    }) | Out-Null

    $previousCpu = $cpuSeconds
    $previousTimestamp = $timestamp

    if ($i -lt $Samples - 1) {
        Start-Sleep -Seconds $IntervalSeconds
    }
}

$results | Format-Table -AutoSize

if (-not [string]::IsNullOrWhiteSpace($CsvPath)) {
    $csvDirectory = Split-Path -Parent $CsvPath
    if (-not [string]::IsNullOrWhiteSpace($csvDirectory)) {
        New-Item -ItemType Directory -Force -Path $csvDirectory | Out-Null
    }

    $results | Export-Csv -LiteralPath $CsvPath -NoTypeInformation -Encoding UTF8
    Write-Host "Saved CSV:"
    Write-Host $CsvPath
}

if ($StopAfter -and $startedProcess -ne $null) {
    try {
        Stop-Process -Id $startedProcess.Id -Force -ErrorAction SilentlyContinue
    }
    catch {
    }
}
