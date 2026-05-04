using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YtMdlp.Models;

namespace YtMdlp.Services;

/// <summary>
/// Thin async wrapper around the bundled <c>yt-dlp.exe</c> binary.
/// All process invocations run off the UI thread (acceptance criterion 7).
/// </summary>
public sealed class YtDlpService
{
    private static readonly Regex PercentRegex = new(@"\[download\]\s+(?<pct>\d+(?:\.\d+)?)%", RegexOptions.Compiled);
    private static readonly Regex DestRegex    = new(@"\[download\]\s+Destination:\s+(?<path>.+)$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex MergerRegex  = new(@"\[Merger\]\s+Merging formats into ""(?<path>[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex ExtractAudio = new(@"\[ExtractAudio\]\s+Destination:\s+(?<path>.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

    public string YtDlpPath => Paths.YtDlpExe;
    public string FfmpegPath => Paths.FfmpegExe;

    public bool IsAvailable => File.Exists(YtDlpPath);

    /// <summary>Auto-update yt-dlp via <c>yt-dlp -U</c> (criterion 5).</summary>
    public Task SelfUpdateAsync(CancellationToken ct = default)
    {
        if (!IsAvailable) return Task.CompletedTask;
        return Task.Run(async () =>
        {
            try
            {
                var psi = BuildPsi(new[] { "-U", "--no-colors" });
                using var p = Process.Start(psi);
                if (p is null) return;
                await p.WaitForExitAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                // Ignore — updates are best-effort.
            }
        }, ct);
    }

    /// <summary>Calls <c>yt-dlp --dump-json &lt;url&gt;</c> and parses the result.</summary>
    public async Task<VideoInfo> DumpInfoAsync(string url, CancellationToken ct = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("yt-dlp.exe is not available. See README to provision it.");

        var psi = BuildPsi(new[] { "--dump-json", "--no-warnings", "--no-colors", url });
        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start yt-dlp.");

        var stdoutTask = p.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (p.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
            throw new InvalidOperationException($"yt-dlp failed: {stderr.Trim()}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        string title = TryString(root, "title") ?? url;
        string channel = TryString(root, "channel") ?? TryString(root, "uploader") ?? "Unknown";
        int durationSec = root.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number
            ? (int)d.GetDouble() : 0;
        string thumb = TryString(root, "thumbnail") ?? "";
        string sourceUrl = TryString(root, "webpage_url") ?? url;

        return new VideoInfo(
            Title: title,
            Channel: channel,
            DurationDisplay: FormatDuration(durationSec),
            DurationSeconds: durationSec,
            ThumbnailUrl: thumb,
            Url: sourceUrl
        );
    }

    /// <summary>
    /// Runs a download and reports progress via <paramref name="progress"/>. Returns the resolved
    /// output file path on success.
    /// </summary>
    public async Task<string> DownloadAsync(
        string url,
        DownloadFormat format,
        QualityPreset quality,
        string outputDir,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("yt-dlp.exe is not available. See README to provision it.");

        Directory.CreateDirectory(outputDir);

        var args = BuildDownloadArgs(url, format, quality, outputDir);
        var psi = BuildPsi(args);

        progress?.Report(new DownloadProgress(DownloadStage.Fetching, null, "Fetching…"));

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start yt-dlp.");

        string? resolvedPath = null;
        var stderrBuf = new StringBuilder();

        var stdoutTask = Task.Run(async () =>
        {
            string? line;
            while ((line = await p.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
            {
                ParseLine(line, progress, ref resolvedPath);
            }
        }, ct);

        var stderrTask = Task.Run(async () =>
        {
            string? line;
            while ((line = await p.StandardError.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
            {
                stderrBuf.AppendLine(line);
            }
        }, ct);

        try
        {
            await p.WaitForExitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
            throw;
        }

        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

        if (p.ExitCode != 0)
        {
            progress?.Report(new DownloadProgress(DownloadStage.Failed, null, "Failed"));
            throw new InvalidOperationException($"yt-dlp exited with code {p.ExitCode}: {stderrBuf}");
        }

        progress?.Report(new DownloadProgress(DownloadStage.Done, 100, "Done", resolvedPath));
        return resolvedPath ?? "";
    }

    private static void ParseLine(string line, IProgress<DownloadProgress>? progress, ref string? resolvedPath)
    {
        if (string.IsNullOrEmpty(line)) return;

        var pctMatch = PercentRegex.Match(line);
        if (pctMatch.Success && double.TryParse(pctMatch.Groups["pct"].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var pct))
        {
            progress?.Report(new DownloadProgress(DownloadStage.Downloading, pct, $"Downloading… {pct:F1}%"));
            return;
        }

        var destMatch = DestRegex.Match(line);
        if (destMatch.Success)
        {
            resolvedPath = destMatch.Groups["path"].Value.Trim();
            return;
        }

        var mergerMatch = MergerRegex.Match(line);
        if (mergerMatch.Success)
        {
            resolvedPath = mergerMatch.Groups["path"].Value.Trim();
            progress?.Report(new DownloadProgress(DownloadStage.Converting, null, "Converting…"));
            return;
        }

        var extractMatch = ExtractAudio.Match(line);
        if (extractMatch.Success)
        {
            resolvedPath = extractMatch.Groups["path"].Value.Trim();
            progress?.Report(new DownloadProgress(DownloadStage.Converting, null, "Converting…"));
            return;
        }

        if (line.Contains("[ffmpeg]", StringComparison.Ordinal))
        {
            progress?.Report(new DownloadProgress(DownloadStage.Converting, null, "Converting…"));
        }
    }

    private static IEnumerable<string> BuildDownloadArgs(
        string url, DownloadFormat format, QualityPreset quality, string outputDir)
    {
        // Output template — yt-dlp expands metadata fields (.%(ext)s).
        string outTemplate = Path.Combine(outputDir, "%(title)s [%(id)s].%(ext)s");

        var args = new List<string>
        {
            "--newline",
            "--no-colors",
            "--no-warnings",
            "--ffmpeg-location", Paths.BinDir,
            "-o", outTemplate,
        };

        switch (format)
        {
            case DownloadFormat.Mp3:
                args.AddRange(new[] { "-x", "--audio-format", "mp3", "--audio-quality", "0" });
                break;

            case DownloadFormat.WebM:
                args.AddRange(new[] { "-f", BuildWebmFormat(quality), "--merge-output-format", "webm" });
                break;

            case DownloadFormat.Custom:
                // Equivalent to "best" — users override via the URL/yt-dlp CLI in the future.
                // TODO: surface a custom-format text box on the Home page.
                args.AddRange(new[] { "-f", "bv*+ba/b" });
                break;

            case DownloadFormat.Mp4:
            default:
                args.AddRange(new[] { "-f", BuildMp4Format(quality), "--merge-output-format", "mp4" });
                break;
        }

        args.Add(url);
        return args;
    }

    private static string BuildMp4Format(QualityPreset quality)
    {
        var max = quality.MaxHeight();
        return max is null
            ? "bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4]/best"
            : $"bv*[ext=mp4][height<={max}]+ba[ext=m4a]/b[ext=mp4][height<={max}]/best[height<={max}]";
    }

    private static string BuildWebmFormat(QualityPreset quality)
    {
        var max = quality.MaxHeight();
        return max is null
            ? "bv*[ext=webm]+ba[ext=webm]/b[ext=webm]/best"
            : $"bv*[ext=webm][height<={max}]+ba[ext=webm]/b[ext=webm][height<={max}]/best[height<={max}]";
    }

    private ProcessStartInfo BuildPsi(IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = YtDlpPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var a in args) psi.ArgumentList.Add(a);
        return psi;
    }

    private static string? TryString(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var el)) return null;
        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }

    private static string FormatDuration(int seconds)
    {
        if (seconds <= 0) return "—";
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }
}
