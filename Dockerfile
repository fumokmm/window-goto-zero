# escape=`
# Windows container image for building the WinForms app.
# Requires Docker Desktop in Windows containers mode.
FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022

WORKDIR C:\src

# Restore as a separate layer when project file changes.
COPY WindowGotoZero.csproj .
RUN dotnet restore WindowGotoZero.csproj

COPY . .
# Self-contained so the host does not need a .NET runtime install.
RUN dotnet publish WindowGotoZero.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o C:\out `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true

# Default command copies build output to a mounted volume.
# Usage: docker compose run --rm build
CMD ["powershell", "-NoProfile", "-Command", "if (Test-Path C:\\dist) { Copy-Item -Path C:\\out\\* -Destination C:\\dist -Recurse -Force; Write-Host 'Published to C:\\dist' } else { Write-Host 'Build output is at C:\\out (mount C:\\dist to copy out)' ; Get-ChildItem C:\\out }"]
