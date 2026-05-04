using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtMdlp.Models;
using YtMdlp.Services;

namespace YtMdlp.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;

    public IReadOnlyList<DownloadFormatItem> FormatOptions { get; } = new[]
    {
        new DownloadFormatItem(DownloadFormat.Mp4),
        new DownloadFormatItem(DownloadFormat.Mp3),
        new DownloadFormatItem(DownloadFormat.WebM),
        new DownloadFormatItem(DownloadFormat.Custom),
    };

    [ObservableProperty]
    private string _defaultOutputFolder;

    [ObservableProperty]
    private DownloadFormatItem _selectedDefaultFormat;

    [ObservableProperty]
    private int _maxConcurrentDownloads;

    [ObservableProperty]
    private bool _autoUpdateYtDlp;

    [ObservableProperty]
    private string? _statusMessage;

    public SettingsViewModel() : this(App.Settings) { }

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;
        _defaultOutputFolder = settings.DefaultOutputFolder;
        _selectedDefaultFormat = FormatOptions.First(f => f.Value == settings.DefaultFormat);
        _maxConcurrentDownloads = settings.MaxConcurrentDownloads;
        _autoUpdateYtDlp = settings.AutoUpdateYtDlp;
    }

    partial void OnDefaultOutputFolderChanged(string value) => _settings.DefaultOutputFolder = value;
    partial void OnSelectedDefaultFormatChanged(DownloadFormatItem value)
    {
        if (value is not null) _settings.DefaultFormat = value.Value;
    }
    partial void OnMaxConcurrentDownloadsChanged(int value) => _settings.MaxConcurrentDownloads = value;
    partial void OnAutoUpdateYtDlpChanged(bool value) => _settings.AutoUpdateYtDlp = value;

    [RelayCommand]
    private async Task PickDefaultFolderAsync()
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
                DefaultOutputFolder = folder.Path;
                StatusMessage = "Default folder updated.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }
}
