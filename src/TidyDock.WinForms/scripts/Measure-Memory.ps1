param(
    [switch]$Launch,
    [switch]$StopAfter,
    [int]$WarmupSeconds = 20,
    [int]$Samples = 6,
    [int]$IntervalSeconds = 5,
    [string]$ExePath = "",
    [string]$CsvPath = ""
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $ExePath = Join-Path $projectRoot "dist\TidyDock-winforms-portable\TidyDock.exe"
}
if ([string]::IsNullOrWhiteSpace($CsvPath)) {
    $CsvPath = Join-Path $projectRoot "dist\memory-winforms.csv"
}

$process = $null
if ($Launch) {
    Get-Process -Name TidyDock -ErrorAction SilentlyContinue | Stop-Process -Force
    $process = Start-Process -FilePath $ExePath -PassThru
    Start-Sleep -Seconds $WarmupSeconds
}
else {
    $process = Get-Process -Name TidyDock -ErrorAction Stop | Select-Object -First 1
}

$rows = @()
for ($i = 0; $i -lt $Samples; $i++) {
    $process.Refresh()
    $rows += [pscustomobject]@{
        Timestamp = Get-Date -Format "o"
        ProcessId = $process.Id
        WorkingSetMB = [math]::Round($process.WorkingSet64 / 1MB, 2)
        PrivateMemoryMB = [math]::Round($process.PrivateMemorySize64 / 1MB, 2)
        Handles = $process.HandleCount
        CPUSeconds = [math]::Round($process.TotalProcessorTime.TotalSeconds, 2)
    }
    Start-Sleep -Seconds $IntervalSeconds
}

$rows | Export-Csv -LiteralPath $CsvPath -NoTypeInformation -Encoding UTF8
$rows | Format-Table
Write-Host "Memory samples:"
Write-Host $CsvPath

if ($StopAfter -and $process -ne $null) {
    Stop-Process -Id $process.Id -Force
}
