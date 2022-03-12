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
    private readonly UiStatus _uiStatus;

    private readonly CompositeDisposable _disposable = new();

    public MainWindowModel(UiStatus uiState, FileExplorerViewModel FileExplorerViewModel)
    {
        _uiStatus = uiState;
        this.FileExplorerViewModel = FileExplorerViewModel;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }
}
