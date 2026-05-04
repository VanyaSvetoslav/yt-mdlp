using Microsoft.UI.Xaml.Controls;
using YtMdlp.ViewModels;

namespace YtMdlp.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel => (HomeViewModel)DataContext;

    public HomePage()
    {
        InitializeComponent();
    }
}
