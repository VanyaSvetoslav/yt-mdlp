using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using YtMdlp.Models;
using YtMdlp.Services;

namespace YtMdlp.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly YtDlpService _ytdlp;
    private readonly SettingsService _settings;
    private readonly DownloadService _downloads;
    private CancellationTokenSource? _downloadCts;

    public IReadOnlyList<DownloadFormatItem> Formats { get; } = new[]
    {
        new DownloadFormatItem(DownloadFormat.Mp4),
        new DownloadFormatItem(DownloadFormat.Mp3),
        new DownloadFormatItem(DownloadFormat.WebM),
        new DownloadFormatItem(DownloadFormat.Custom),
    };

    public IReadOnlyList<QualityPresetItem> Qualities { get; } = new[]
    {
        new QualityPresetItem(QualityPreset.Best),
        new QualityPresetItem(QualityPreset.P1080),
        new QualityPresetItem(QualityPreset.P720),
        new QualityPresetItem(QualityPreset.P480),
    };

    [ObservableProperty]
    private string _url = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsQualityVisible))]
    [NotifyPropertyChangedFor(nameof(QualityVisibility))]
    private DownloadFormatItem _selectedFormat;

    [ObservableProperty]
    private QualityPresetItem _selectedQuality;

    [ObservableProperty]
    private string _outputFolder = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFetchedInfo))]
    [NotifyPropertyChangedFor(nameof(FetchedInfoVisibility))]
    private VideoInfo? _fetchedInfo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsBusyVisibility))]
    [NotifyPropertyChangedFor(nameof(IsIdleVisibility))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIndeterminate))]
    [NotifyPropertyChangedFor(nameof(ProgressValue))]
    private double? _progressPercent;

    public double ProgressValue => ProgressPercent ?? 0d;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ErrorVisibility))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    [NotifyPropertyChangedFor(nameof(SuccessVisibility))]
    private string? _successMessage;

    public bool IsIdle => !IsBusy;
    public bool HasFetchedInfo => FetchedInfo is not null;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);
    public bool IsQualityVisible => !SelectedFormat.Value.IsAudioOnly();
    public bool IsIndeterminate => ProgressPercent is null;

    public Visibility QualityVisibility => IsQualityVisible ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FetchedInfoVisibility => HasFetchedInfo ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsBusyVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsIdleVisibility => IsIdle ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ErrorVisibility => HasError ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SuccessVisibility => HasSuccess ? Visibility.Visible : Visibility.Collapsed;

    public HomeViewModel()
        : this(App.YtDlp, App.Settings, App.Downloads) { }

    public HomeViewModel(YtDlpService ytdlp, SettingsService settings, DownloadService downloads)
    {
        _ytdlp = ytdlp;
        _settings = settings;
        _downloads = downloads;

        _selectedFormat = Formats.First(f => f.Value == settings.DefaultFormat);
        _selectedQuality = Qualities[0];
        _outputFolder = settings.DefaultOutputFolder;
    }

    [RelayCommand]
    private async Task FetchInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            ErrorMessage = "Paste a YouTube link first.";
            return;
        }

        ErrorMessage = null;
        SuccessMessage = null;
        FetchedInfo = null;
        IsBusy = true;
        StatusText = "Fetching video info…";
        ProgressPercent = null;

        try
        {
            FetchedInfo = await _ytdlp.DumpInfoAsync(Url.Trim()).ConfigureAwait(true);
            StatusText = "Ready";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusText = "Failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            ErrorMessage = "Paste a YouTube link first.";
            return;
        }
        if (string.IsNullOrWhiteSpace(OutputFolder))
        {
            ErrorMessage = "Choose an output folder.";
            return;
        }

        ErrorMessage = null;
        SuccessMessage = null;
        IsBusy = true;
        ProgressPercent = null;
        StatusText = "Fetching…";

        var progress = new Progress<DownloadProgress>(p =>
        {
            StatusText = p.Status;
            ProgressPercent = p.Percent;
        });

        _downloadCts = new CancellationTokenSource();
        try
        {
            var path = await _ytdlp.DownloadAsync(
                Url.Trim(),
                SelectedFormat.Value,
                SelectedQuality.Value,
                OutputFolder,
                progress,
                _downloadCts.Token).ConfigureAwait(true);

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                _downloads.Record(new DownloadHistoryEntry
                {
                    FullPath = path,
                    Format = SelectedFormat.Value.ToString(),
                    SourceUrl = Url.Trim(),
                });
                StatusText = "Done ✓";
                SuccessMessage = $"Saved to {path}";
            }
            else
            {
                StatusText = "Done ✓";
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusText = "Failed";
        }
        finally
        {
            IsBusy = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    [RelayCommand]
    private void Cancel() => _downloadCts?.Cancel();

    [RelayCommand]
    private async Task PickOutputFolderAsync()
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
            {
                OutputFolder = folder.Path;
                _settings.DefaultOutputFolder = folder.Path;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    partial void OnSelectedFormatChanged(DownloadFormatItem value)
    {
        if (value is not null) _settings.DefaultFormat = value.Value;
    }
}
