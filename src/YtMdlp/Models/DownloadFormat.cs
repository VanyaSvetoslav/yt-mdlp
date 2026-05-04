namespace YtMdlp.Models;

public enum DownloadFormat
{
    Mp4,
    Mp3,
    WebM,
    Custom,
}

public static class DownloadFormatExtensions
{
    public static string DisplayName(this DownloadFormat f) => f switch
    {
        DownloadFormat.Mp4 => "MP4 (best quality)",
        DownloadFormat.Mp3 => "MP3 (audio only)",
        DownloadFormat.WebM => "WebM",
        DownloadFormat.Custom => "Custom",
        _ => f.ToString(),
    };

    public static bool IsAudioOnly(this DownloadFormat f) => f == DownloadFormat.Mp3;
}
