#Requires -Version 5.1
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$PidFile   = Join-Path $ScriptDir 'app.pid'

if (-not (Test-Path $PidFile)) {
    Write-Host "No PID file found ($PidFile). IntegrationHub.Api is not running (or was not started via start_hub.ps1)." -ForegroundColor Yellow
    exit 0
}

$rawPid = Get-Content $PidFile -Raw | ForEach-Object { $_.Trim() }

if (-not $rawPid) {
    Write-Host "PID file is empty. Removing." -ForegroundColor Yellow
    Remove-Item $PidFile -Force
    exit 0
}

$targetPid = $rawPid -as [int]
if (-not $targetPid) {
    Write-Error "PID file contains invalid value: '$rawPid'"
    Remove-Item $PidFile -Force
    exit 1
}

try {
    $proc = Get-Process -Id $targetPid -ErrorAction Stop

    Stop-Process -Id $targetPid -Force
    $proc.WaitForExit(5000) | Out-Null

    Write-Host "IntegrationHub.Api (PID $targetPid) stopped." -ForegroundColor Green
} catch [Microsoft.PowerShell.Commands.ProcessCommandException] {
    Write-Host "Process with PID $targetPid is not running (already stopped)." -ForegroundColor Yellow
} catch {
    Write-Error "Failed to stop process (PID $targetPid): $_"
    exit 1
} finally {
    if (Test-Path $PidFile) {
        Remove-Item $PidFile -Force
    }
}
