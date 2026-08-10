#Requires -Version 5.1
<#
.SYNOPSIS
  Install a project-local (portable) .NET 8 SDK under .tools\dotnet if missing.
  Does not modify machine-wide installs or the user PATH permanently.
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$installDir = Join-Path $root ".tools\dotnet"
$dotnet = Join-Path $installDir "dotnet.exe"

function Test-SdkReady {
    param([string]$DotnetPath)
    if (-not (Test-Path $DotnetPath)) {
        return $false
    }
    $tfms = & $DotnetPath --list-sdks 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $false
    }
    return ($tfms | Where-Object { $_ -match '^8\.' }).Count -gt 0
}

if (Test-SdkReady $dotnet) {
    Write-Host "Portable .NET SDK ready: $dotnet" -ForegroundColor Green
    & $dotnet --list-sdks | ForEach-Object { Write-Host $_ }
    exit 0
}

Write-Host "Installing portable .NET 8 SDK into $installDir ..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $installDir | Out-Null

$scriptPath = Join-Path $env:TEMP "dotnet-install.ps1"
$uri = "https://dot.net/v1/dotnet-install.ps1"
Invoke-WebRequest -Uri $uri -OutFile $scriptPath -UseBasicParsing

# Note: dotnet-install.ps1 may leave a non-zero LASTEXITCODE even on success.
# Trust the presence of a working SDK rather than the script exit code.
& $scriptPath -Channel 8.0 -InstallDir $installDir

if (-not (Test-SdkReady $dotnet)) {
    throw "SDK install finished but .NET 8 SDK was not found under $installDir"
}

Write-Host "Portable .NET SDK installed." -ForegroundColor Green
& $dotnet --list-sdks | ForEach-Object { Write-Host $_ }
