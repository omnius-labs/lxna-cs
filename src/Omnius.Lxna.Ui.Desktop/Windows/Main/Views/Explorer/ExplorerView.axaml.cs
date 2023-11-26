using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Omnius.Core.Avalonia;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public interface IExplorerViewCommands
{
    void ThumbnailGeneratorsScrollToTop();
}

public class ExplorerView : StatefulUserControl<ExplorerViewModelBase>, IExplorerViewCommands
{
    private readonly ItemsRepeater _treeNodesRepeater;
    private readonly ScrollViewer _ThumbnailGeneratorsViewer;
    private readonly ItemsRepeater _ThumbnailGeneratorsRepeater;

    public ExplorerView()
    {
        this.InitializeComponent();

        _treeNodesRepeater = this.FindControl<ItemsRepeater>("TreeNodesRepeater");
        _ThumbnailGeneratorsViewer = this.FindControl<ScrollViewer>("ThumbnailGeneratorsViewer");
        _ThumbnailGeneratorsRepeater = this.FindControl<ItemsRepeater>("ThumbnailGeneratorsRepeater");

        this.GetObservable(ViewModelProperty).Subscribe(this.OnViewModelChanged);
        _treeNodesRepeater.Tapped += this.OnTreeNodeTapped;
        _ThumbnailGeneratorsRepeater.DoubleTapped += this.OnThumbnailDoubleTapped;
        _ThumbnailGeneratorsRepeater.ElementPrepared += this.OnThumbnailPrepared;
        _ThumbnailGeneratorsRepeater.ElementClearing += this.OnThumbnailClearing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void ThumbnailGeneratorsScrollToTop()
    {
        _ThumbnailGeneratorsViewer.ScrollToHome();
    }

    private void OnViewModelChanged(ExplorerViewModelBase? viewModel)
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
