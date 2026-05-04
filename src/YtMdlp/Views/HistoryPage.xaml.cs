using Microsoft.UI.Xaml.Controls;
using YtMdlp.Models;
using YtMdlp.ViewModels;

namespace YtMdlp.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel => (HistoryViewModel)DataContext;

    public HistoryPage()
    {
        InitializeComponent();
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is DownloadHistoryEntry entry)
        {
            ViewModel.OpenInExplorerCommand.Execute(entry);
        }
    }
}
