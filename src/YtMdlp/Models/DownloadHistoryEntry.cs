using System;

namespace YtMdlp.Models;

public sealed class DownloadHistoryEntry
{
    public string Filename { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Format { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTimeOffset Date { get; set; }
    public string SourceUrl { get; set; } = "";

    public string SizeDisplay => FormatSize(SizeBytes);
    public string DateDisplay => Date.LocalDateTime.ToString("yyyy-MM-dd HH:mm");

    private static string FormatSize(long bytes)
    {
        const long KB = 1024, MB = KB * 1024, GB = MB * 1024;
        return bytes switch
        {
            >= GB => $"{bytes / (double)GB:F2} GB",
            >= MB => $"{bytes / (double)MB:F1} MB",
            >= KB => $"{bytes / (double)KB:F0} KB",
            _ => $"{bytes} B",
        };
    }
}
