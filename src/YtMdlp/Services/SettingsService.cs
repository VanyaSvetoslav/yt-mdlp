using System;
using System.IO;
using System.Text.Json;
using YtMdlp.Models;

namespace YtMdlp.Services;

/// <summary>
/// Persists app settings to <c>ApplicationData.LocalSettings</c> when the app is packaged
/// (acceptance criterion 6). Falls back to a JSON file under <c>%LocalAppData%\yt-mdlp\</c>
/// when running unpackaged (criterion 10 fallback path), where <c>ApplicationData.Current</c>
/// is unavailable.
/// </summary>
public sealed class SettingsService
{
    private const string KeyDefaultOutputFolder = "DefaultOutputFolder";
    private const string KeyDefaultFormat = "DefaultFormat";
    private const string KeyMaxConcurrent = "MaxConcurrentDownloads";
    private const string KeyAutoUpdate = "AutoUpdateYtDlp";
    private const string KeyHistoryJson = "HistoryJson";

    private readonly bool _useLocalSettings;
    private readonly string _jsonPath;
    private AppSettings _cache = new();
    private string _historyJson = "[]";

    public SettingsService()
    {
        _jsonPath = Path.Combine(Paths.AppDataDir, "settings.json");
        Directory.CreateDirectory(Paths.AppDataDir);

        try
        {
            // Probe LocalSettings — throws COMException for unpackaged apps without identity.
            _ = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            _useLocalSettings = true;
        }
        catch
        {
            _useLocalSettings = false;
        }

        Load();

        if (string.IsNullOrWhiteSpace(_cache.DefaultOutputFolder))
        {
            _cache.DefaultOutputFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "yt-mdlp");
        }
    }

    public string DefaultOutputFolder
    {
        get => _cache.DefaultOutputFolder;
        set { _cache.DefaultOutputFolder = value; Save(); }
    }

    public DownloadFormat DefaultFormat
    {
        get => _cache.DefaultFormat;
        set { _cache.DefaultFormat = value; Save(); }
    }

    public int MaxConcurrentDownloads
    {
        get => Math.Clamp(_cache.MaxConcurrentDownloads, 1, 4);
        set { _cache.MaxConcurrentDownloads = Math.Clamp(value, 1, 4); Save(); }
    }

    public bool AutoUpdateYtDlp
    {
        get => _cache.AutoUpdateYtDlp;
        set { _cache.AutoUpdateYtDlp = value; Save(); }
    }

    public string HistoryJson
    {
        get => _historyJson;
        set { _historyJson = value ?? "[]"; SaveHistory(); }
    }

    private void Load()
    {
        if (_useLocalSettings)
        {
            var v = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            if (v[KeyDefaultOutputFolder] is string s1) _cache.DefaultOutputFolder = s1;
            if (v[KeyDefaultFormat] is string s2 && Enum.TryParse<DownloadFormat>(s2, out var f)) _cache.DefaultFormat = f;
            if (v[KeyMaxConcurrent] is int i1) _cache.MaxConcurrentDownloads = i1;
            if (v[KeyAutoUpdate] is bool b1) _cache.AutoUpdateYtDlp = b1;
            if (v[KeyHistoryJson] is string s3) _historyJson = s3;
        }
        else if (File.Exists(_jsonPath))
        {
            try
            {
                var raw = File.ReadAllText(_jsonPath);
                var loaded = JsonSerializer.Deserialize<PersistedSettings>(raw);
                if (loaded is not null)
                {
                    _cache = loaded.Settings ?? new AppSettings();
                    _historyJson = loaded.HistoryJson ?? "[]";
                }
            }
            catch
            {
                // Fall through to defaults — never crash on malformed settings.
            }
        }
    }

    private void Save()
    {
        if (_useLocalSettings)
        {
            var v = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            v[KeyDefaultOutputFolder] = _cache.DefaultOutputFolder;
            v[KeyDefaultFormat] = _cache.DefaultFormat.ToString();
            v[KeyMaxConcurrent] = _cache.MaxConcurrentDownloads;
            v[KeyAutoUpdate] = _cache.AutoUpdateYtDlp;
        }
        else
        {
            WriteJsonFile();
        }
    }

    private void SaveHistory()
    {
        if (_useLocalSettings)
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[KeyHistoryJson] = _historyJson;
        }
        else
        {
            WriteJsonFile();
        }
    }

    private void WriteJsonFile()
    {
        var data = new PersistedSettings { Settings = _cache, HistoryJson = _historyJson };
        var raw = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_jsonPath, raw);
    }

    private sealed class PersistedSettings
    {
        public AppSettings? Settings { get; set; }
        public string? HistoryJson { get; set; }
    }
}
