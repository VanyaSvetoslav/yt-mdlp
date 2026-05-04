<#
.SYNOPSIS
    Downloads yt-dlp.exe and ffmpeg.exe into src/YtMdlp/Assets/bin/.

.DESCRIPTION
    yt-dlp and ffmpeg are not committed to this repository (binaries are large
    and have their own update cadence/license terms). Run this script once
    before building, or rely on the app's first-launch provisioning step that
    copies whatever is present in Assets/bin/ to %LocalAppData%\yt-mdlp\bin\.

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
    Write-Host "  yt-dlp.exe already present — skipping."
}

# ffmpeg — BtbN essentials (Windows x64) zip; we extract ffmpeg.exe + ffprobe.exe.
$ffmpegZipUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-win64-gpl.zip"
$ffmpegExeDst = Join-Path $OutDir "ffmpeg.exe"
$ffprobeExeDst = Join-Path $OutDir "ffprobe.exe"

if (-not (Test-Path $ffmpegExeDst) -or -not (Test-Path $ffprobeExeDst)) {
    $tmpZip = Join-Path ([System.IO.Path]::GetTempPath()) "ffmpeg-bootstrap.zip"
    $tmpDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ffmpeg-bootstrap-" + [Guid]::NewGuid())
    Download-File -Url $ffmpegZipUrl -Destination $tmpZip
    New-Item -ItemType Directory -Force -Path $tmpDir | Out-Null
    Expand-Archive -Path $tmpZip -DestinationPath $tmpDir -Force

    $ffmpegSrc = Get-ChildItem -Path $tmpDir -Recurse -Filter "ffmpeg.exe"  | Select-Object -First 1
    $ffprobeSrc = Get-ChildItem -Path $tmpDir -Recurse -Filter "ffprobe.exe" | Select-Object -First 1
    if (-not $ffmpegSrc -or -not $ffprobeSrc) {
        throw "Could not locate ffmpeg.exe / ffprobe.exe in extracted archive."
    }
    Copy-Item -Path $ffmpegSrc.FullName  -Destination $ffmpegExeDst  -Force
    Copy-Item -Path $ffprobeSrc.FullName -Destination $ffprobeExeDst -Force

    Remove-Item -Path $tmpZip -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $tmpDir -Recurse -Force -ErrorAction SilentlyContinue
} else {
    Write-Host "  ffmpeg.exe / ffprobe.exe already present — skipping."
}

Write-Host "`nBootstrap complete:"
Get-ChildItem -Path $OutDir -File | ForEach-Object { Write-Host "  $($_.Name)  ($([Math]::Round($_.Length/1MB,1)) MB)" }
