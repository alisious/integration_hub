#Requires -Version 5.1
<#
.SYNOPSIS
    Builds and packages IntegrationHub.Api as a self-contained offline deployment archive.

.DESCRIPTION
    1. Runs "dotnet publish" (win-x64, self-contained, Release)
    2. Copies start_hub.ps1 / stop_hub.ps1 into the package
    3. Zips everything into dist\IntegrationHub.Api-<version>-<sha>-win-x64.zip

.PARAMETER OutputDir
    Directory where the final .zip is placed. Defaults to <repo>\dist.

.PARAMETER NoBuild
    Skip dotnet publish and repackage whatever is already in the publish folder.

.EXAMPLE
    .\scripts\pack_hub.ps1
    .\scripts\pack_hub.ps1 -OutputDir C:\deploy\packages
#>
param(
    [string] $OutputDir = 'C:\dev\deploy\integration_hub',
    [switch] $NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# --- paths ---
$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot    = Split-Path -Parent $ScriptDir
$ProjectPath = [System.IO.Path]::Combine($RepoRoot, 'src', 'Core', 'Api', 'IntegrationHub.Api.csproj')
$PublishDir  = [System.IO.Path]::Combine($RepoRoot, 'src', 'Core', 'Api', 'bin', 'Release', 'net8.0', 'win-x64', 'publish')


# --- version from Directory.Build.props ---
$buildProps = [System.IO.Path]::Combine($RepoRoot, 'Directory.Build.props')
$version = '0.0.0'
if (Test-Path $buildProps) {
    $xml = [xml](Get-Content $buildProps -Raw -Encoding UTF8)
    $v = $xml.SelectSingleNode('//Version')
    if ($v) { $version = $v.InnerText.Trim() }
}

# --- git short SHA ---
$sha = 'unknown'
try {
    $sha = (& git -C $RepoRoot rev-parse --short HEAD 2>$null).Trim()
} catch { }

$packageName = "IntegrationHub.Api-$version-$sha-win-x64"
Write-Host "Package : $packageName" -ForegroundColor Cyan

# --- dotnet publish ---
if (-not $NoBuild) {
    Write-Host "`nRunning dotnet publish..." -ForegroundColor Cyan
    $publishArgs = @(
        'publish', $ProjectPath,
        '--configuration', 'Release',
        '--runtime', 'win-x64',
        '--self-contained', 'true',
        '--output', $PublishDir,
        '-p:PublishSingleFile=false',
        '-p:PublishReadyToRun=false'
    )
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed (exit code $LASTEXITCODE)."
        exit 1
    }
    Write-Host "Publish succeeded." -ForegroundColor Green
} else {
    Write-Host "Skipping build (-NoBuild)." -ForegroundColor Yellow
    if (-not (Test-Path $PublishDir)) {
        Write-Error "Publish folder not found: $PublishDir"
        exit 1
    }
}

# --- assemble staging folder ---
$stagingRoot   = [System.IO.Path]::Combine($env:TEMP, 'IntegrationHub_staging', $packageName)
$stagingPublish = [System.IO.Path]::Combine($stagingRoot, 'publish')
$stagingScripts = [System.IO.Path]::Combine($stagingRoot, 'scripts')

if (Test-Path $stagingRoot) { Remove-Item $stagingRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stagingPublish | Out-Null
New-Item -ItemType Directory -Path $stagingScripts | Out-Null

# copy published binaries
Write-Host "`nCopying published files..."
Copy-Item -Path "$PublishDir\*" -Destination $stagingPublish -Recurse -Force

# copy management scripts (exclude runtime artifacts)
$scriptsToCopy = @('start_hub.ps1', 'stop_hub.ps1')
foreach ($s in $scriptsToCopy) {
    $src = [System.IO.Path]::Combine($ScriptDir, $s)
    if (Test-Path $src) {
        Copy-Item $src -Destination $stagingScripts -Force
        Write-Host "  + scripts\$s"
    } else {
        Write-Warning "Script not found, skipping: $src"
    }
}

# --- write version manifest ---
$manifest = [System.IO.Path]::Combine($stagingRoot, 'version.txt')
@"
Package  : $packageName
Version  : $version
Commit   : $sha
Built    : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Runtime  : win-x64 self-contained (.NET 8)
"@ | Set-Content $manifest -Encoding UTF8
Write-Host "  + version.txt"

# --- zip ---
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }
$zipPath = [System.IO.Path]::Combine($OutputDir, "$packageName.zip")
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Write-Host "`nCreating archive..."
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stagingRoot, $zipPath)

# --- cleanup staging ---
Remove-Item $stagingRoot -Recurse -Force

$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host "`nDone." -ForegroundColor Green
Write-Host "  Archive : $zipPath"
Write-Host "  Size    : $sizeMB MB"
