using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Omnius.Core.Avalonia;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public interface IExplorerViewCommands
{
    void ThumbnailsScrollToTop();
}

public partial class ExplorerView : UserControl, IExplorerViewCommands
{
    private readonly ListBox _treeNodesListBox;
    private readonly ScrollViewer _thumbnailsViewer;
    private readonly ItemsRepeater _thumbnailsRepeater;

    public ExplorerView()
    {
        this.InitializeComponent();

        _treeNodesListBox = this.FindControl<ListBox>("TreeNodeListBox") ?? throw new NullReferenceException();
        _thumbnailsViewer = this.FindControl<ScrollViewer>("ThumbnailsViewer") ?? throw new NullReferenceException();
        _thumbnailsRepeater = this.FindControl<ItemsRepeater>("ThumbnailsRepeater") ?? throw new NullReferenceException();

        this.DataContextChanged += this.OnDataContextChanged;
        _treeNodesListBox.SelectionChanged += this.OnTreeNodeSelectionChanged;
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

    private void OnTreeNodeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.DataContext is ExplorerViewModel viewModel)
        {
            if (_treeNodesListBox.SelectedItem is TreeNodeModel model)
            {
                viewModel.NotifyTreeNodeTapped(model);
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

    private void OnThumbnailPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (this.DataContext is ExplorerViewModel viewModel)
        {
            if (e.Element.DataContext is null) return;
            viewModel.NotifyThumbnailPrepared(e.Element.DataContext);
        }
    }

    private void OnThumbnailClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (this.DataContext is ExplorerViewModel viewModel)
        {
            if (e.Element.DataContext is null) return;
            viewModel.NotifyThumbnailClearing(e.Element.DataContext);
        }
    }
}
