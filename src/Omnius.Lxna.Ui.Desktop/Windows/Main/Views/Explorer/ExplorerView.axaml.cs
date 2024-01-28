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
    private readonly ScrollViewer _ThumbnailsViewer;
    private readonly ItemsRepeater _ThumbnailsRepeater;

    public ExplorerView()
    {
        this.InitializeComponent();

        _treeNodesRepeater = this.FindControl<ItemsRepeater>("TreeNodesRepeater");
        _ThumbnailsViewer = this.FindControl<ScrollViewer>("ThumbnailsViewer");
        _ThumbnailsRepeater = this.FindControl<ItemsRepeater>("ThumbnailsRepeater");

        this.DataContextChanged += this.OnDataContextChanged;
        _treeNodesRepeater.Tapped += this.OnTreeNodeTapped;
        _ThumbnailsRepeater.DoubleTapped += this.OnThumbnailDoubleTapped;
        _ThumbnailsRepeater.ElementPrepared += this.OnThumbnailPrepared;
        _ThumbnailsRepeater.ElementClearing += this.OnThumbnailClearing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void ThumbnailsScrollToTop()
    {
        _ThumbnailsViewer.ScrollToHome();
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
