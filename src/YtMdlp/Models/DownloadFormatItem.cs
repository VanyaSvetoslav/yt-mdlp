namespace YtMdlp.Models;

/// <summary>
/// ComboBox-friendly wrapper around <see cref="DownloadFormat"/>. WinUI 3 ComboBoxes display
/// items via <c>ToString()</c> when no <c>ItemTemplate</c> is provided; this gives us the
/// "MP4 (best quality)" / "MP3 (audio only)" labels from the spec without an extra converter.
/// </summary>
public sealed class DownloadFormatItem
{
    public DownloadFormat Value { get; }
    public string Display { get; }

    public DownloadFormatItem(DownloadFormat value)
    {
        Value = value;
        Display = value.DisplayName();
    }

    public override string ToString() => Display;

    public override bool Equals(object? obj) => obj is DownloadFormatItem o && o.Value == Value;
    public override int GetHashCode() => Value.GetHashCode();
}

public sealed class QualityPresetItem
{
    public QualityPreset Value { get; }
    public string Display { get; }

    public QualityPresetItem(QualityPreset value)
    {
        Value = value;
        Display = value.DisplayName();
    }

    public override string ToString() => Display;

    public override bool Equals(object? obj) => obj is QualityPresetItem o && o.Value == Value;
    public override int GetHashCode() => Value.GetHashCode();
}
