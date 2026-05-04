namespace YtMdlp.Models;

public sealed record VideoInfo(
    string Title,
    string Channel,
    string DurationDisplay,
    int DurationSeconds,
    string ThumbnailUrl,
    string Url
);
