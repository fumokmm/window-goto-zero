#Requires -Version 5.1
<#
.SYNOPSIS
  Build the app, then produce an Inno Setup installer under installer\output\
#>
$ErrorActionPreference = "Stop"
Set-Location (Split-Path -Parent $PSScriptRoot)

Write-Host "==> Building application..." -ForegroundColor Cyan
& "$PSScriptRoot\build.ps1"
if ($LASTEXITCODE -ne 0) {
    throw "Application build failed."
}

$exe = Join-Path (Get-Location) "dist\WindowGotoZero.exe"
if (-not (Test-Path $exe)) {
    throw "Missing $exe"
}

Write-Host "==> Ensuring Inno Setup compiler..." -ForegroundColor Cyan
& "$PSScriptRoot\ensure-inno.ps1"
if ($LASTEXITCODE -ne 0) {
    throw "ensure-inno.ps1 failed."
}

$iscc = Join-Path (Get-Location) ".tools\inno\ISCC.exe"
if (-not (Test-Path $iscc)) {
    $found = Get-ChildItem -Path ".\.tools\inno" -Filter "ISCC.exe" -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $found) {
        throw "ISCC.exe not found."
    }
    $iscc = $found.FullName
}

$iss = Join-Path (Get-Location) "installer\WindowGotoZero.iss"
$outputDir = Join-Path (Get-Location) "installer\output"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

Write-Host "==> Compiling installer..." -ForegroundColor Cyan
& $iscc $iss
if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed."
}

$setup = Get-ChildItem -Path $outputDir -Filter "WindowGotoZero-Setup-*.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $setup) {
    throw "Installer exe not found under $outputDir"
}

Write-Host ""
Write-Host "Installer OK: $($setup.FullName)" -ForegroundColor Green
Write-Host "Double-click to install (per-user, no admin required by default)."
