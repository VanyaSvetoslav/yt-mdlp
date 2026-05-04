using System;
using System.IO;
using System.Threading.Tasks;

namespace YtMdlp.Services;

/// <summary>
/// On first launch, mirrors the bundled binaries (yt-dlp.exe + the BtbN ffmpeg
/// shared build: ffmpeg.exe, ffprobe.exe, avcodec-*.dll, avformat-*.dll, etc.)
/// from the app's <c>Assets/bin/</c> directory into
/// <c>%LocalAppData%\yt-mdlp\bin\</c> so they can be invoked from a stable,
/// writable location (acceptance criterion 8). All files are mirrored — not
/// just the .exes — because ffmpeg.exe in the shared build resolves its
/// codec/format DLLs from the directory it lives in.
/// </summary>
public sealed class BinaryProvisioner
{
    public Task EnsureProvisionedAsync()
    {
        return Task.Run(() =>
        {
            Directory.CreateDirectory(Paths.BinDir);

            var assetsBin = Path.Combine(AppContext.BaseDirectory, "Assets", "bin");
            if (!Directory.Exists(assetsBin)) return;

            foreach (var src in Directory.EnumerateFiles(assetsBin, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(assetsBin, src);
                if (string.Equals(Path.GetFileName(rel), ".gitkeep", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dest = Path.Combine(Paths.BinDir, rel);
                CopyIfMissing(src, dest);
            }
        });
    }

    private static void CopyIfMissing(string source, string dest)
    {
        try
        {
            if (File.Exists(dest)) return;
            if (!File.Exists(source)) return;
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
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
