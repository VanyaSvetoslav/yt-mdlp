namespace YtMdlp.Models;

/// <summary>
/// Plain DTO mirrored to <c>ApplicationData.LocalSettings</c> via <see cref="Services.SettingsService"/>.
/// </summary>
public sealed class AppSettings
{
    public string DefaultOutputFolder { get; set; } = "";
    public DownloadFormat DefaultFormat { get; set; } = DownloadFormat.Mp4;
    public int MaxConcurrentDownloads { get; set; } = 2;
    public bool AutoUpdateYtDlp { get; set; } = true;
}
