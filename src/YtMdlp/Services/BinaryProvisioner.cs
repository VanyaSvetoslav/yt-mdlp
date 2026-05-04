using System;
using System.IO;
using System.Threading.Tasks;

namespace YtMdlp.Services;

/// <summary>
/// On first launch, copies bundled <c>yt-dlp.exe</c> / <c>ffmpeg.exe</c> from the app's
/// <c>Assets/bin/</c> directory into <c>%LocalAppData%\yt-mdlp\bin\</c> so they can be
/// invoked from a stable, writable location (acceptance criterion 8).
/// </summary>
public sealed class BinaryProvisioner
{
    public Task EnsureProvisionedAsync()
    {
        return Task.Run(() =>
        {
            Directory.CreateDirectory(Paths.BinDir);

            var assetsBin = Path.Combine(AppContext.BaseDirectory, "Assets", "bin");
            CopyIfMissing(Path.Combine(assetsBin, "yt-dlp.exe"), Paths.YtDlpExe);
            CopyIfMissing(Path.Combine(assetsBin, "ffmpeg.exe"), Paths.FfmpegExe);
            CopyIfMissing(Path.Combine(assetsBin, "ffprobe.exe"), Path.Combine(Paths.BinDir, "ffprobe.exe"));
        });
    }

    private static void CopyIfMissing(string source, string dest)
    {
        try
        {
            if (File.Exists(dest)) return;
            if (!File.Exists(source)) return;
            File.Copy(source, dest, overwrite: false);
        }
        catch
        {
            // Provisioning is best-effort. The user is warned via InfoBar in HomePage if yt-dlp is missing.
        }
    }

    public bool IsYtDlpAvailable => File.Exists(Paths.YtDlpExe);
    public bool IsFfmpegAvailable => File.Exists(Paths.FfmpegExe);
}
