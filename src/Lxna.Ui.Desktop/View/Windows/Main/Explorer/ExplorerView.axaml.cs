using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Omnius.Core.Base;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public interface IExplorerViewCommands
{
    void ThumbnailsViewerScrollToTop();
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
        _thumbnailsRepeater.ElementPrepared += this.OnThumbnailElementPrepared;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        // _thumbnailsRepeater.ItemsSourceView!.CollectionChanged += this.OnThumbnailsRepeaterItemsChanged;
    }

    public void ThumbnailsViewerScrollToTop()
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
        if (this.DataContext is ExplorerViewModel viewModel)
        {
            if (e.Source is IDataContextProvider control)
            {
                if (control.DataContext is null) return;
                viewModel.NotifyTreeNodeTapped(control.DataContext);
            }
        }
    }

    private void OnThumbnailDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (this.DataContext is ExplorerViewModel viewModel)
        {
            if (e.Source is IDataContextProvider control)
            {
                if (control.DataContext is null) return;
                viewModel.NotifyThumbnailDoubleTapped(control.DataContext);
            }
        }
    }

    private void OnThumbnailElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (this.DataContext is ExplorerViewModel viewModel)
        {
            var items = _thumbnailsRepeater.GetLogicalChildren().Cast<IDataContextProvider>().Select(n => n.DataContext).WhereNotNull().ToList();
            viewModel.NotifyThumbnailsChanged(items);
        }
    }
}
