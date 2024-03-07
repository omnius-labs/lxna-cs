using System.Reactive.Disposables;
using Avalonia.Controls;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public class ExplorerViewDesignModel : ExplorerViewModelBase
{
    private readonly CompositeDisposable _disposable = new();

    public ExplorerViewDesignModel()
    {
        var _isBusy = new ReactivePropertySlim<bool>(false).AddTo(_disposable);
        this.IsWaiting = _isBusy.ToReadOnlyReactivePropertySlim().AddTo(_disposable);
        this.TreeViewWidth = new ReactivePropertySlim<GridLength>(new GridLength(240)).AddTo(_disposable);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }

    public override void NotifyThumbnailClearing(object item) { }
    public override void NotifyThumbnailDoubleTapped(object item) { }
    public override void NotifyThumbnailPrepared(object item) { }
    public override void NotifyTreeNodeTapped(object item) { }
    public override void SetViewCommands(IExplorerViewCommands commands) { }
}
