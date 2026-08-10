#Requires -Version 5.1
<#
.SYNOPSIS
  Ensure Inno Setup 6 compiler (ISCC.exe) is available under .tools\inno.
  Uses a silent local install into the repo tools folder (not Program Files).
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$innoDir = Join-Path $root ".tools\inno"
$iscc = Join-Path $innoDir "ISCC.exe"

if (Test-Path $iscc) {
    Write-Host "Inno Setup ready: $iscc" -ForegroundColor Green
    exit 0
}

Write-Host "Installing Inno Setup 6 into $innoDir ..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $innoDir | Out-Null

# Immutable GitHub release (download.php returns HTML in non-browser clients)
$installer = Join-Path $env:TEMP "innosetup-6.7.3.exe"
$uri = "https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe"

Write-Host "Downloading $uri ..."
Invoke-WebRequest -Uri $uri -OutFile $installer -UseBasicParsing

# Basic sanity check: real PE executable, not an HTML error page
$header = Get-Content -Path $installer -Encoding Byte -TotalCount 2
if ($header[0] -ne 0x4D -or $header[1] -ne 0x5A) {
    throw "Downloaded file is not a Windows executable (got HTML or corrupt data)."
}
$size = (Get-Item $installer).Length
if ($size -lt 1MB) {
    throw "Downloaded installer is unexpectedly small ($size bytes)."
}

$argList = @(
    "/VERYSILENT",
    "/SUPPRESSMSGBOXES",
    "/NORESTART",
    "/SP-",
    "/DIR=$innoDir"
)

$proc = Start-Process -FilePath $installer -ArgumentList $argList -Wait -PassThru
if ($proc.ExitCode -ne 0) {
    throw "Inno Setup installer failed with exit code $($proc.ExitCode)."
}

if (-not (Test-Path $iscc)) {
    # Some versions nest under a subfolder
    $found = Get-ChildItem -Path $innoDir -Filter "ISCC.exe" -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($found) {
        Write-Host "Inno Setup ready: $($found.FullName)" -ForegroundColor Green
        exit 0
    }
    throw "ISCC.exe not found under $innoDir after install."
}

Write-Host "Inno Setup ready: $iscc" -ForegroundColor Green
