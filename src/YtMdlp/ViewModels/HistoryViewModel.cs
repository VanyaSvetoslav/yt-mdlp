using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtMdlp.Models;
using YtMdlp.Services;

namespace YtMdlp.ViewModels;

public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly DownloadService _downloads;

    public ObservableCollection<DownloadHistoryEntry> Items => _downloads.History;

    public HistoryViewModel() : this(App.Downloads) { }

    public HistoryViewModel(DownloadService downloads)
    {
        _downloads = downloads;
    }

    /// <summary>Open the file's containing folder in Explorer with the file selected (criterion 4).</summary>
    [RelayCommand]
    private void OpenInExplorer(DownloadHistoryEntry? entry)
    {
        if (entry is null || string.IsNullOrEmpty(entry.FullPath)) return;
        try
        {
            if (File.Exists(entry.FullPath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{entry.FullPath}\"")
                {
                    UseShellExecute = true,
                });
            }
            else
            {
                var dir = Path.GetDirectoryName(entry.FullPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
                }
            }
        }
        catch
        {
            // Best-effort.
        }
    }

    [RelayCommand]
    private void Clear() => _downloads.Clear();
}
