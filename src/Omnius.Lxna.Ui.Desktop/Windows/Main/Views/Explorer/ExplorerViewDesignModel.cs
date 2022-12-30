using System.Reactive.Disposables;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public class ExplorerViewDesignModel : ExplorerViewModelBase
{
    private readonly CompositeDisposable _disposable = new();

    public ExplorerViewDesignModel()
    {
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
