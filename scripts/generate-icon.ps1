#Requires -Version 5.1
<#
.SYNOPSIS
  Generate Assets\app.ico (multi-size) and Assets\app-preview.png (256px).
#>
$ErrorActionPreference = "Stop"
Set-Location (Split-Path -Parent $PSScriptRoot)

$assets = Join-Path (Get-Location) "Assets"
New-Item -ItemType Directory -Force -Path $assets | Out-Null
$outIco = Join-Path $assets "app.ico"
$outPng = Join-Path $assets "app-preview.png"
$src = Join-Path (Get-Location) "scripts\IconGenerator\IconGenerator.cs"
$exe = Join-Path $env:TEMP "wgz-gen-icon.exe"

$cscCandidates = @(
    "${env:WINDIR}\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "${env:WINDIR}\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw "csc.exe not found. Cannot generate icon."
}

Write-Host "Compiling icon generator with $csc ..." -ForegroundColor Cyan
& $csc /nologo /target:exe /optimize+ /out:$exe /r:System.Drawing.dll $src
if ($LASTEXITCODE -ne 0) { throw "Icon generator compile failed." }

Write-Host "Generating icon + preview ..." -ForegroundColor Cyan
& $exe $outIco $outPng
if ($LASTEXITCODE -ne 0) { throw "Icon generator failed." }

Get-Item $outIco, $outPng | Format-Table Name, Length -AutoSize
Write-Host "Icon ready: $outIco" -ForegroundColor Green
Write-Host "Preview:    $outPng" -ForegroundColor Green
