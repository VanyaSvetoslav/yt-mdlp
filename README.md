# yt-mdlp — pre-built Windows binaries

This branch is **auto-generated** by [.github/workflows/release.yml](https://github.com/VanyaSvetoslav/yt-mdlp/blob/main/.github/workflows/release.yml).
Do not commit changes here directly — they will be overwritten on the next push to `main`.

- **Built from commit:** [`c9cfc32`](https://github.com/VanyaSvetoslav/yt-mdlp/commit/c9cfc3294c134908d3c96231e153e35b6960b6a8)
- **Built at:** 2026-05-04T19:52:21Z
- **Configuration:** Release / x64
- **Runtime:** self-contained .NET 8 + Windows App SDK 1.7

## How to run

1. Click the green **"Code"** button above and download this branch as a zip
   (or clone it).
2. Extract and run `YtMdlp.exe`.

The bundled `Assets/bin/yt-dlp.exe` + `ffmpeg.exe` are extracted to
`%LocalAppData%\yt-mdlp\bin\` on first launch.

## Source

Source code lives on the
[`main`](https://github.com/VanyaSvetoslav/yt-mdlp/tree/main) branch.

## Licensing

- `YtMdlp.exe` and the project source: MIT (see
  [`LICENSE`](https://github.com/VanyaSvetoslav/yt-mdlp/blob/main/LICENSE)
  on `main`).
- Bundled `yt-dlp.exe` — Unlicense (public domain). See
  <https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE>.
- Bundled `ffmpeg.exe` — **GPL v3** (BtbN's GPL build of FFmpeg).
  See <https://www.ffmpeg.org/legal.html>.
