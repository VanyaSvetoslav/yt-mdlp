# yt-mdlp

A native-feeling **Windows 11** desktop YouTube downloader that wraps
[`yt-dlp`](https://github.com/yt-dlp/yt-dlp) in a Fluent-Design WinUI 3 GUI.

> **Platform:** Windows 11 (build 22621 / 22H2 or newer) — **only**.
> Windows 10 / macOS / Linux are explicitly out of scope.

## Features

- Single-window shell with `NavigationView` (compact-left mode) and **Mica** backdrop
- Three pages: **Home**, **History**, **Settings**
- Paste a YouTube URL, press **Fetch Info** → see thumbnail, title, channel, duration
- Format selector: **MP4 (best quality)**, **MP3 (audio only)**, **WebM**, **Custom**
- Quality selector for video formats: **Best**, **1080p**, **720p**, **480p**
- Folder picker for output directory
- **Download** runs in the background — UI never freezes
- Real-time progress (indeterminate ➜ percentage from yt-dlp output) and a status
  line: *Fetching… → Downloading… → Converting… → Done ✓*
- Downloads history persists across sessions; click a row to open the file's
  folder in Explorer
- Settings: default output folder, default format, max concurrent downloads
  (1–4), yt-dlp auto-update toggle (`yt-dlp -U` on launch)
- All settings persisted via `ApplicationData.LocalSettings` (with a JSON
  fallback when the app is run unpackaged)
- Light / dark theme follows system; accent color inherited from system
- Errors and successes are surfaced via `InfoBar` (no `MessageBox`); icons
  come from the `Segoe Fluent Icons` font

## Screenshots

> _Screenshots are added once the first build is verified on a Windows host._

| Home | History | Settings |
| ---- | ------- | -------- |
| _todo_ | _todo_ | _todo_ |

## Architecture

| Layer       | Tech                                                         |
| ----------- | ------------------------------------------------------------ |
| Language    | C# 12 / .NET 8                                               |
| UI          | WinUI 3 via Windows App SDK 1.7 (latest stable 1.7 release)  |
| Pattern     | MVVM (`CommunityToolkit.Mvvm` 8.x — `[ObservableProperty]`, `[RelayCommand]`) |
| Backdrop    | Fluent `MicaBackdrop` (uses `MicaController` under the hood) |
| Backend     | Bundled `yt-dlp.exe` invoked via `System.Diagnostics.Process`|
| Audio merge | Bundled `ffmpeg.exe` (used by yt-dlp via `--ffmpeg-location`) |
| Storage     | `Windows.Storage.ApplicationData.LocalSettings` (packaged) / JSON fallback (unpackaged) |
| Packaging   | **Unpackaged** self-contained WinUI 3 (MSIX is the planned future path; see *Build*) |

```
yt-mdlp/
├── src/YtMdlp/
│   ├── Assets/bin/        # yt-dlp.exe + ffmpeg.exe (NOT committed)
│   ├── Models/            # POCOs / enums
│   ├── Services/          # YtDlpService, DownloadService, SettingsService, BinaryProvisioner
│   ├── ViewModels/        # HomeViewModel, HistoryViewModel, SettingsViewModel
│   ├── Views/             # HomePage, HistoryPage, SettingsPage (XAML + code-behind)
│   ├── App.xaml(.cs)
│   ├── MainWindow.xaml(.cs)
│   └── YtMdlp.csproj
├── scripts/bootstrap.ps1  # Downloads yt-dlp + ffmpeg into Assets/bin/
├── .github/workflows/build.yml
├── LICENSE                # MIT
└── README.md
```

## Build

### Prerequisites

- Windows 11 22H2 (build 22621) or newer
- Either:
  - **Visual Studio 2022 17.9+** with the **".NET Multi-platform App UI"** /
    **"Windows App SDK"** workload, **or**
  - The standalone **.NET 8 SDK** (`winget install Microsoft.DotNet.SDK.8`).
- (Once.) `pwsh` (PowerShell 7+) is recommended for running `bootstrap.ps1`,
  but the bundled `powershell.exe` (Windows PowerShell 5.1) also works.

### Provision the bundled binaries

`yt-dlp.exe` and `ffmpeg.exe` are not checked in (they are large and have
their own update cadence / license terms). Run the bootstrap script once
before building:

```powershell
pwsh -File scripts/bootstrap.ps1
```

This downloads:

- `yt-dlp.exe` — the official Windows single-file build from
  <https://github.com/yt-dlp/yt-dlp/releases/latest>
- `ffmpeg.exe` and `ffprobe.exe` — extracted from the Win64 GPL build on
  <https://github.com/BtbN/FFmpeg-Builds/releases/latest>

You can also drop the binaries into `src/YtMdlp/Assets/bin/` manually.
On first launch the app copies whatever it finds there into
`%LocalAppData%\yt-mdlp\bin\`, so subsequent runs always invoke a stable,
writable copy.

### Build & run

```powershell
# Restore, build, and run the app
dotnet restore src\YtMdlp\YtMdlp.csproj /p:Platform=x64
dotnet build   src\YtMdlp\YtMdlp.csproj -c Release /p:Platform=x64
dotnet run     --project src\YtMdlp\YtMdlp.csproj /p:Platform=x64

# Produce a self-contained, unpackaged binary tree under publish/
dotnet publish src\YtMdlp\YtMdlp.csproj -c Release /p:Platform=x64 `
    -r win-x64 --self-contained true -o publish\x64
.\publish\x64\YtMdlp.exe
```

### MSIX packaging (optional)

The project ships unpackaged by default (acceptance criterion 10's
fallback path). To produce an MSIX you can either:

1. Set `<WindowsPackageType>MSIX</WindowsPackageType>` and
   `<EnableMsixTooling>true</EnableMsixTooling>` in `YtMdlp.csproj`,
   add a `Package.appxmanifest`, and build via
   `dotnet build -c Release /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true`.
2. Or use Visual Studio's **Project → Publish → Create App Packages…** wizard.

### CI

[`.github/workflows/build.yml`](./.github/workflows/build.yml) runs on every
push and PR against `main`. It executes `dotnet restore` / `build` / `publish`
on a `windows-latest` runner and uploads the published self-contained artifact.

## License

This project is licensed under the [MIT License](./LICENSE).

### Bundled third-party binaries

The published / bootstrapped binaries fall under their own licenses:

- **[yt-dlp](https://github.com/yt-dlp/yt-dlp)** — released into the public
  domain via the Unlicense.
  See <https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE>.
- **[FFmpeg](https://ffmpeg.org/)** — licensed under the **GPL v3** (when
  using the GPL build from BtbN). The user is responsible for complying with
  GPL terms when distributing the bundled `ffmpeg.exe`.
  See <https://www.ffmpeg.org/legal.html>.

This repository does **not** redistribute either binary; the bootstrap
script downloads them from their official upstream release feeds at build
time.

## Out of scope

- macOS / Linux support
- Any browser extension
- Telemetry / analytics
- Playlist batch downloads (left as a `TODO` comment in
  `Services/YtDlpService.cs`)
