using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using YtMdlp.ViewModels;

namespace YtMdlp.Views;

public sealed partial class SettingsPage : Page, INotifyPropertyChanged
{
    public SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

    public bool HasStatus => !string.IsNullOrEmpty(ViewModel?.StatusMessage);

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (ViewModel is null) return;
            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SettingsViewModel.StatusMessage))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasStatus)));
                }
            };
        };
    }
}
