#Requires -Version 5.1
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot    = Split-Path -Parent $ScriptDir
$PidFile     = Join-Path $ScriptDir 'app.pid'
$LogFile     = Join-Path $ScriptDir 'app.log'
$PublishedDll = Join-Path $RepoRoot 'publish' 'IntegrationHub.Api.dll'
$ProjectPath  = Join-Path $RepoRoot 'src' 'Core' 'Api' 'IntegrationHub.Api.csproj'

# --- guard: already running ---
if (Test-Path $PidFile) {
    $existingPid = Get-Content $PidFile -Raw | ForEach-Object { $_.Trim() }
    if ($existingPid -and (Get-Process -Id $existingPid -ErrorAction SilentlyContinue)) {
        Write-Host "IntegrationHub.Api is already running (PID $existingPid)." -ForegroundColor Yellow
        exit 0
    }
    Remove-Item $PidFile -Force
}

# --- choose launch mode ---
if (Test-Path $PublishedDll) {
    $exe  = 'dotnet'
    $args = @($PublishedDll)
    $mode = "published DLL ($PublishedDll)"
} elseif (Test-Path $ProjectPath) {
    $exe  = 'dotnet'
    $args = @('run', '--project', $ProjectPath, '--no-launch-profile')
    $mode = "dotnet run ($ProjectPath)"
} else {
    Write-Error "Cannot find project file or published DLL. Searched:`n  $ProjectPath`n  $PublishedDll"
    exit 1
}

# --- start detached ---
try {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName               = $exe
    $psi.Arguments              = $args -join ' '
    $psi.WorkingDirectory       = $RepoRoot
    $psi.UseShellExecute        = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError  = $true
    $psi.CreateNoWindow         = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    # Async log forwarding
    $logWriter = [System.IO.StreamWriter]::new($LogFile, $false, [System.Text.Encoding]::UTF8)
    $logWriter.AutoFlush = $true

    $onOutput = {
        if ($null -ne $EventArgs.Data) {
            $Event.MessageData.WriteLine("[{0}] {1}" -f (Get-Date -Format 'HH:mm:ss'), $EventArgs.Data)
        }
    }

    Register-ObjectEvent -InputObject $proc -EventName OutputDataReceived `
        -Action $onOutput -MessageData $logWriter | Out-Null
    Register-ObjectEvent -InputObject $proc -EventName ErrorDataReceived `
        -Action $onOutput -MessageData $logWriter | Out-Null

    $null = $proc.Start()
    $proc.BeginOutputReadLine()
    $proc.BeginErrorReadLine()

    $proc.Id | Set-Content -Path $PidFile -Encoding UTF8

    Write-Host "IntegrationHub.Api started ($mode)." -ForegroundColor Green
    Write-Host "  PID : $($proc.Id)"
    Write-Host "  Log : $LogFile"
    Write-Host "  PID file: $PidFile"
} catch {
    Write-Error "Failed to start IntegrationHub.Api: $_"
    exit 1
}
