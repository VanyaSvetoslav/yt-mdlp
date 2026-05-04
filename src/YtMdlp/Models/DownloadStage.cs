namespace YtMdlp.Models;

public enum DownloadStage
{
    Idle,
    Fetching,
    Downloading,
    Converting,
    Done,
    Failed,
}

public sealed record DownloadProgress(
    DownloadStage Stage,
    double? Percent,
    string Status,
    string? OutputPath = null
);
