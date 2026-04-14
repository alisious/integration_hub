#Requires -Version 5.1
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$ScriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot     = Split-Path -Parent $ScriptDir
$PidFile      = [System.IO.Path]::Combine($ScriptDir, 'app.pid')
$LogFile      = [System.IO.Path]::Combine($ScriptDir, 'app.log')
$ErrFile      = [System.IO.Path]::Combine($ScriptDir, 'app.err.log')
$PublishedDll = [System.IO.Path]::Combine($RepoRoot, 'publish', 'IntegrationHub.Api.dll')
$ProjectPath  = [System.IO.Path]::Combine($RepoRoot, 'src', 'Core', 'Api', 'IntegrationHub.Api.csproj')

# --- guard: already running ---
if (Test-Path $PidFile) {
    $existingPid = (Get-Content $PidFile -Raw).Trim()
    if ($existingPid -and (Get-Process -Id $existingPid -ErrorAction SilentlyContinue)) {
        Write-Host "IntegrationHub.Api is already running (PID $existingPid)." -ForegroundColor Yellow
        exit 0
    }
    Remove-Item $PidFile -Force
}

# --- choose launch mode ---
if (Test-Path $PublishedDll) {
    $dotnetArgs = "`"$PublishedDll`""
    $mode = "published DLL ($PublishedDll)"
} elseif (Test-Path $ProjectPath) {
    $dotnetArgs = "run --project `"$ProjectPath`" --no-launch-profile"
    $mode = "dotnet run ($ProjectPath)"
} else {
    Write-Error "Cannot find project file or published DLL.`n  Searched: $ProjectPath`n  Searched: $PublishedDll"
    exit 1
}

# --- start detached via Start-Process ---
try {
    $proc = Start-Process `
        -FilePath 'dotnet' `
        -ArgumentList $dotnetArgs `
        -WorkingDirectory $RepoRoot `
        -RedirectStandardOutput $LogFile `
        -RedirectStandardError  $ErrFile `
        -WindowStyle Hidden `
        -PassThru `
        -NoNewWindow

    $proc.Id | Set-Content -Path $PidFile -Encoding UTF8

    # --- resolve Swagger URL from launchSettings.json ---
    $launchSettings = [System.IO.Path]::Combine($RepoRoot, 'src', 'Core', 'Api', 'Properties', 'launchSettings.json')
    $swaggerUrl = 'http://localhost:5266/swagger'
    if (Test-Path $launchSettings) {
        $ls = Get-Content $launchSettings -Raw | ConvertFrom-Json
        $appUrl = $ls.profiles.http.applicationUrl
        if ($appUrl) {
            $baseUrl = ($appUrl -split ';' | Where-Object { $_ -like 'http://*' } | Select-Object -First 1).TrimEnd('/')
            $swaggerUrl = "$baseUrl/swagger"
        }
    }

    Write-Host "IntegrationHub.Api started ($mode)." -ForegroundColor Green
    Write-Host "  PID      : $($proc.Id)"
    Write-Host "  Log      : $LogFile"
    Write-Host "  Err log  : $ErrFile"
    Write-Host "  PID file : $PidFile"
    Write-Host ""
    Write-Host "  Swagger  : $swaggerUrl" -ForegroundColor Cyan
} catch {
    Write-Error "Failed to start IntegrationHub.Api: $_"
    exit 1
}
