using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Omnius.Core.Avalonia;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public interface IFileExplorerViewCommands
{
    void ScrollToTop();
}

public class FileExplorerView : StatefulUserControl<FileExplorerViewModelBase>, IFileExplorerViewCommands
{
    private readonly ScrollViewer _scrollViewer;
    private readonly ItemsRepeater _itemsRepeater;

    public FileExplorerView()
    {
        this.InitializeComponent();

        _scrollViewer = this.FindControl<ScrollViewer>("ScrollViewer");
        _itemsRepeater = this.FindControl<ItemsRepeater>("ItemsRepeater");

        this.GetObservable(ViewModelProperty).Subscribe(this.OnViewModelChanged);

        _itemsRepeater.DoubleTapped += this.ItemsRepeater_DoubleTapped;
        _itemsRepeater.ElementPrepared += this.ItemsRepeater_ElementPrepared;
        _itemsRepeater.ElementClearing += this.ItemsRepeater_ElementClearing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void ScrollToTop()
    {
        _scrollViewer.ScrollToHome();
    }

    private void OnViewModelChanged(FileExplorerViewModelBase? viewModel)
    {
        viewModel?.SetViewCommands(this);
    }

    private void ItemsRepeater_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (e.Source is IDataContextProvider control)
        {
            if (control.DataContext is null) return;
            this.ViewModel?.NotifyThumbnailDoubleTapped(control.DataContext);
        }
    }

    private void ItemsRepeater_ElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element.DataContext is null) return;
        this.ViewModel?.NotifyThumbnailPrepared(e.Element.DataContext);
    }

    private void ItemsRepeater_ElementClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element.DataContext is null) return;
        this.ViewModel?.NotifyThumbnailClearing(e.Element.DataContext);
    }
}
