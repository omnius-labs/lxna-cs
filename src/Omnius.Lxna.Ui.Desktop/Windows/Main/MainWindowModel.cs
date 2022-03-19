using Omnius.Core;
using Omnius.Lxna.Ui.Desktop.Configuration;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public abstract class MainWindowModelBase : AsyncDisposableBase
{
    public MainWindowStatus? Status { get; protected set; }

    public FileExplorerViewModelBase? FileExplorerViewModel { get; protected set; }
}

public class MainWindowModel : MainWindowModelBase
{
    private readonly CompositeDisposable _disposable = new();

    public MainWindowModel(UiStatus uiStatus, FileExplorerViewModel FileExplorerViewModel)
    {
        this.Status = uiStatus.MainWindow ??= new MainWindowStatus();
        this.FileExplorerViewModel = FileExplorerViewModel;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }
}
