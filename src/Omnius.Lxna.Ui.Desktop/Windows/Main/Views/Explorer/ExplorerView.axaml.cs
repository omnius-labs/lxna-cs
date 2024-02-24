using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Omnius.Core.Avalonia;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public interface IExplorerViewCommands
{
    void ThumbnailsScrollToTop();
}

public partial class ExplorerView : UserControl, IExplorerViewCommands
{
    private readonly ItemsRepeater _treeNodesRepeater;
    private readonly ScrollViewer _thumbnailsViewer;
    private readonly ItemsRepeater _thumbnailsRepeater;

    public ExplorerView()
    {
        this.InitializeComponent();

        _treeNodesRepeater = this.FindControl<ItemsRepeater>("TreeNodesRepeater") ?? throw new NullReferenceException();
        _thumbnailsViewer = this.FindControl<ScrollViewer>("ThumbnailsViewer") ?? throw new NullReferenceException();
        _thumbnailsRepeater = this.FindControl<ItemsRepeater>("ThumbnailsRepeater") ?? throw new NullReferenceException();

        this.DataContextChanged += this.OnDataContextChanged;
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

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (this.DataContext is ExplorerViewModel viewModel)
        {
            viewModel.SetViewCommands(this);
        }
    }

    private void OnTreeNodeTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (e.Source is IDataContextProvider control)
        {
            if (control.DataContext is null) return;
            if (this.DataContext is ExplorerViewModel viewModel)
            {
                viewModel.NotifyTreeNodeTapped(control.DataContext);
            }
        }
    }

    private void OnThumbnailDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (e.Source is IDataContextProvider control)
        {
            if (control.DataContext is null) return;
            if (this.DataContext is ExplorerViewModel viewModel)
            {
                viewModel.NotifyThumbnailDoubleTapped(control.DataContext);
            }
        }
    }

    private void OnThumbnailPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element.DataContext is null) return;
        if (this.DataContext is ExplorerViewModel viewModel)
        {
            viewModel.NotifyThumbnailPrepared(e.Element.DataContext);
        }
    }

    private void OnThumbnailClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element.DataContext is null) return;
        if (this.DataContext is ExplorerViewModel viewModel)
        {
            viewModel.NotifyThumbnailClearing(e.Element.DataContext);
        }
    }
}
