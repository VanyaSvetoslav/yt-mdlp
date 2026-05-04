using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using YtMdlp.Models;

namespace YtMdlp.Services;

/// <summary>
/// Tracks completed downloads and persists them via <see cref="SettingsService"/>.
/// History is exposed as an <see cref="ObservableCollection{T}"/> so the
/// History page binds to it without a refresh round-trip.
/// </summary>
public sealed class DownloadService
{
    public ObservableCollection<DownloadHistoryEntry> History { get; } = new();

    public DownloadService()
    {
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        try
        {
            var raw = App.Settings.HistoryJson;
            if (string.IsNullOrWhiteSpace(raw)) return;
            var list = JsonSerializer.Deserialize<List<DownloadHistoryEntry>>(raw);
            if (list is null) return;
            foreach (var item in list) History.Add(item);
        }
        catch
        {
            // Ignore corruption — history is non-critical.
        }
    }

    public void Record(DownloadHistoryEntry entry)
    {
        if (string.IsNullOrEmpty(entry.FullPath)) return;
        if (string.IsNullOrEmpty(entry.Filename))
            entry.Filename = Path.GetFileName(entry.FullPath);
        if (entry.SizeBytes == 0 && File.Exists(entry.FullPath))
            entry.SizeBytes = new FileInfo(entry.FullPath).Length;
        if (entry.Date == default)
            entry.Date = DateTimeOffset.Now;

        History.Insert(0, entry);
        Persist();
    }

    public void Clear()
    {
        History.Clear();
        Persist();
    }

    private void Persist()
    {
        var raw = JsonSerializer.Serialize(History);
        App.Settings.HistoryJson = raw;
    }
}
