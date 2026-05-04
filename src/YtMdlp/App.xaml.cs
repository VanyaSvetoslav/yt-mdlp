using Microsoft.UI.Xaml;
using YtMdlp.Services;

namespace YtMdlp;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public static SettingsService Settings { get; } = new SettingsService();
    public static BinaryProvisioner BinaryProvisioner { get; } = new BinaryProvisioner();
    public static YtDlpService YtDlp { get; } = new YtDlpService();
    public static DownloadService Downloads { get; } = new DownloadService();

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            // Surface the message in the debugger; UI-level errors are reported via InfoBar in the pages.
            System.Diagnostics.Debug.WriteLine($"Unhandled: {e.Exception}");
            e.Handled = true;
        };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Provision bundled yt-dlp.exe / ffmpeg.exe to %LocalAppData%\yt-mdlp\bin\ on first launch.
        await BinaryProvisioner.EnsureProvisionedAsync();

        // Kick off auto-update of yt-dlp if the user has it enabled (criterion 5).
        if (Settings.AutoUpdateYtDlp)
        {
            _ = YtDlp.SelfUpdateAsync();
        }

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
