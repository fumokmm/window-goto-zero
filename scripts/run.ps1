#Requires -Version 5.1
<#
.SYNOPSIS
  Launch the built GUI from .\dist (must run on the Windows host, not in a container).
#>
$ErrorActionPreference = "Stop"
Set-Location (Split-Path -Parent $PSScriptRoot)

$exe = Join-Path (Get-Location) "dist\WindowGotoZero.exe"
if (-not (Test-Path $exe)) {
    Write-Host "Executable not found. Building first..." -ForegroundColor Yellow
    & "$PSScriptRoot\build.ps1"
}

Write-Host "Starting $exe" -ForegroundColor Cyan
Start-Process -FilePath $exe
