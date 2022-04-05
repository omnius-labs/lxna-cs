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
    public MainWindowModel(UiStatus uiStatus, FileExplorerViewModel FileExplorerViewModel)
    {
        this.Status = uiStatus.MainWindow ??= new MainWindowStatus();
        this.FileExplorerViewModel = FileExplorerViewModel;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        if (this.FileExplorerViewModel is not null) await this.FileExplorerViewModel.DisposeAsync();
    }
}
