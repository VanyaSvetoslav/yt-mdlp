<#
.SYNOPSIS
    Downloads yt-dlp.exe and the BtbN ffmpeg "shared" GPL build into
    src/YtMdlp/Assets/bin/.

.DESCRIPTION
    yt-dlp and ffmpeg are not committed to this repository (binaries are large
    and have their own update cadence/license terms). Run this script once
    before building, or rely on the app's first-launch provisioning step that
    mirrors whatever is present in Assets/bin/ to %LocalAppData%\yt-mdlp\bin\.

    We use BtbN's *shared* GPL build (ffmpeg-master-latest-win64-gpl-shared.zip)
    instead of the static one, because individual GitHub-pushable files are
    capped at 100 MB. The static ffmpeg.exe / ffprobe.exe are ~190 MB each;
    the shared build's exes are tiny (~700 KB) with the heavy lifting in a
    handful of avcodec/avfilter/avformat/etc. DLLs (each well under 100 MB),
    which lets us push the published tree to a regular Git branch.

.PARAMETER OutDir
    Override the destination directory (default: src/YtMdlp/Assets/bin).

.EXAMPLE
    pwsh -File scripts/bootstrap.ps1
#>

[CmdletBinding()]
param(
    [string] $OutDir = (Join-Path $PSScriptRoot "..\src\YtMdlp\Assets\bin")
)

$ErrorActionPreference = "Stop"

$OutDir = (Resolve-Path -LiteralPath (New-Item -ItemType Directory -Force -Path $OutDir)).Path
Write-Host "Bootstrapping binaries into: $OutDir"

function Download-File {
    param(
        [Parameter(Mandatory=$true)] [string] $Url,
        [Parameter(Mandatory=$true)] [string] $Destination
    )
    Write-Host "  Downloading $Url"
    Invoke-WebRequest -Uri $Url -OutFile $Destination -UseBasicParsing
}

# yt-dlp — official Windows build (yt-dlp.exe, single-file).
$ytDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
$ytDlpDst = Join-Path $OutDir "yt-dlp.exe"
if (-not (Test-Path $ytDlpDst)) {
    Download-File -Url $ytDlpUrl -Destination $ytDlpDst
} else {
    Write-Host "  yt-dlp.exe already present - skipping."
}

# ffmpeg - BtbN shared GPL build. The shared zip ships ffmpeg.exe + ffprobe.exe
# alongside avcodec-*.dll / avformat-*.dll / avfilter-*.dll / etc. We copy the
# entire bin/ directory so that ffmpeg.exe can resolve its DLLs at runtime.
$ffmpegZipUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-win64-gpl-shared.zip"
$ffmpegExeDst = Join-Path $OutDir "ffmpeg.exe"
$ffprobeExeDst = Join-Path $OutDir "ffprobe.exe"

# Treat the bundle as "already provisioned" only if both exes AND at least one
# avcodec DLL are present. Otherwise re-extract.
$existingDll = Get-ChildItem -Path $OutDir -Filter "avcodec-*.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
if ((Test-Path $ffmpegExeDst) -and (Test-Path $ffprobeExeDst) -and $existingDll) {
    Write-Host "  ffmpeg shared build already present - skipping."
}
else {
    $tmpZip = Join-Path ([System.IO.Path]::GetTempPath()) "ffmpeg-bootstrap.zip"
    $tmpDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ffmpeg-bootstrap-" + [Guid]::NewGuid())
    Download-File -Url $ffmpegZipUrl -Destination $tmpZip
    New-Item -ItemType Directory -Force -Path $tmpDir | Out-Null
    Expand-Archive -Path $tmpZip -DestinationPath $tmpDir -Force

    # Locate the bin/ folder inside the extracted archive (top-level dir name
    # varies by build date, e.g. ffmpeg-N-119123-...).
    $binSrc = Get-ChildItem -Path $tmpDir -Recurse -Directory -Filter "bin" |
              Where-Object { Test-Path (Join-Path $_.FullName "ffmpeg.exe") } |
              Select-Object -First 1
    if (-not $binSrc) {
        throw "Could not locate bin/ffmpeg.exe in the extracted archive ($tmpDir)."
    }

    Write-Host "  Copying contents of $($binSrc.FullName) to $OutDir"
    Get-ChildItem -Path $binSrc.FullName -File | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination (Join-Path $OutDir $_.Name) -Force
    }

    # Also copy the LICENSE.txt next to the binaries for GPL compliance.
    $licenseSrc = Get-ChildItem -Path $tmpDir -Recurse -Filter "LICENSE.txt" -ErrorAction SilentlyContinue |
                  Select-Object -First 1
    if ($licenseSrc) {
        Copy-Item -Path $licenseSrc.FullName -Destination (Join-Path $OutDir "ffmpeg-LICENSE.txt") -Force
    }

    Remove-Item -Path $tmpZip -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $tmpDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "`nBootstrap complete:"
Get-ChildItem -Path $OutDir -File | ForEach-Object { Write-Host ("  {0}  ({1} MB)" -f $_.Name, [Math]::Round($_.Length/1MB, 1)) }
