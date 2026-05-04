using System;
using System.IO;

namespace YtMdlp.Services;

/// <summary>
/// Centralised filesystem locations.
/// </summary>
public static class Paths
{
    public static string AppDataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "yt-mdlp");

    public static string BinDir { get; } = Path.Combine(AppDataDir, "bin");

    public static string YtDlpExe { get; } = Path.Combine(BinDir, "yt-dlp.exe");
    public static string FfmpegExe { get; } = Path.Combine(BinDir, "ffmpeg.exe");
}
