#Requires -Version 5.1
<#
.SYNOPSIS
  Build WindowGotoZero with a project-local portable .NET SDK (no machine install).

.PARAMETER SelfContained
  Bundle the .NET runtime into the exe (~150MB). Default is framework-dependent (~few MB),
  which requires the .NET 8 Desktop Runtime on the target PC.
#>
param(
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
Set-Location (Split-Path -Parent $PSScriptRoot)

Write-Host "==> Ensuring portable SDK..." -ForegroundColor Cyan
& "$PSScriptRoot\ensure-sdk.ps1"
if ($LASTEXITCODE -ne 0) {
    throw "ensure-sdk.ps1 failed."
}

$dotnet = Join-Path (Get-Location) ".tools\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    throw "dotnet.exe not found at $dotnet"
}

# Clean so a previous self-contained build cannot leave a huge leftover exe.
if (Test-Path ".\dist") {
    Remove-Item ".\dist\*" -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Force -Path ".\dist" | Out-Null

Write-Host "==> Restoring..." -ForegroundColor Cyan
& $dotnet restore .\WindowGotoZero.csproj
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

if ($SelfContained) {
    Write-Host "==> Publishing (self-contained win-x64, large)..." -ForegroundColor Cyan
    & $dotnet publish .\WindowGotoZero.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -o .\dist `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:DebugType=none `
        /p:DebugSymbols=false
}
else {
    Write-Host "==> Publishing (framework-dependent win-x64, small)..." -ForegroundColor Cyan
    & $dotnet publish .\WindowGotoZero.csproj `
        -c Release `
        -r win-x64 `
        --self-contained false `
        -o .\dist `
        /p:PublishSingleFile=true `
        /p:DebugType=none `
        /p:DebugSymbols=false
}

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

$exe = Join-Path (Get-Location) "dist\WindowGotoZero.exe"
if (-not (Test-Path $exe)) {
    throw "Build finished but $exe was not found."
}

$mb = [math]::Round((Get-Item $exe).Length / 1MB, 2)
Write-Host ""
Write-Host "Build OK: $exe ($mb MB)" -ForegroundColor Green
if (-not $SelfContained) {
    Write-Host "Note: target PC needs .NET 8 Desktop Runtime (x64)." -ForegroundColor Yellow
    Write-Host "  https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"
}
Write-Host "Run with: .\scripts\run.ps1"
