using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Omnius.Core.Avalonia;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public interface IFileExplorerViewCommands
{
    void ThumbnailsScrollToTop();
}

public class FileExplorerView : StatefulUserControl<FileExplorerViewModelBase>, IFileExplorerViewCommands
{
    private readonly ItemsRepeater _treeNodesRepeater;
    private readonly ScrollViewer _thumbnailsViewer;
    private readonly ItemsRepeater _thumbnailsRepeater;

    public FileExplorerView()
    {
        this.InitializeComponent();

        _treeNodesRepeater = this.FindControl<ItemsRepeater>("TreeNodesRepeater");
        _thumbnailsViewer = this.FindControl<ScrollViewer>("ThumbnailsViewer");
        _thumbnailsRepeater = this.FindControl<ItemsRepeater>("ThumbnailsRepeater");

        this.GetObservable(ViewModelProperty).Subscribe(this.OnViewModelChanged);
        _treeNodesRepeater.Tapped += this.OnTreeNodeTapped;
        _thumbnailsRepeater.DoubleTapped += this.OnThumbnailDoubleTapped;
        _thumbnailsRepeater.ElementPrepared += this.OnThumbnailPrepared;
        _thumbnailsRepeater.ElementClearing += this.OnThumbnailClearing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void ThumbnailsScrollToTop()
    {
        _thumbnailsViewer.ScrollToHome();
    }

    private void OnViewModelChanged(FileExplorerViewModelBase? viewModel)
    {
        viewModel?.SetViewCommands(this);
    }

    private void OnTreeNodeTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (e.Source is IDataContextProvider control)
        {
            if (control.DataContext is null) return;
            this.ViewModel?.NotifyTreeNodeTapped(control.DataContext);
        }
    }

    private void OnThumbnailDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (e.Source is IDataContextProvider control)
        {
            if (control.DataContext is null) return;
            this.ViewModel?.NotifyThumbnailDoubleTapped(control.DataContext);
        }
    }

    private void OnThumbnailPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element.DataContext is null) return;
        this.ViewModel?.NotifyThumbnailPrepared(e.Element.DataContext);
    }

    private void OnThumbnailClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element.DataContext is null) return;
        this.ViewModel?.NotifyThumbnailClearing(e.Element.DataContext);
    }
}
