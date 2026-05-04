namespace YtMdlp.Models;

public enum QualityPreset
{
    Best,
    P1080,
    P720,
    P480,
}

public static class QualityPresetExtensions
{
    public static string DisplayName(this QualityPreset q) => q switch
    {
        QualityPreset.Best => "Best",
        QualityPreset.P1080 => "1080p",
        QualityPreset.P720 => "720p",
        QualityPreset.P480 => "480p",
        _ => q.ToString(),
    };

    public static int? MaxHeight(this QualityPreset q) => q switch
    {
        QualityPreset.Best => null,
        QualityPreset.P1080 => 1080,
        QualityPreset.P720 => 720,
        QualityPreset.P480 => 480,
        _ => null,
    };
}
