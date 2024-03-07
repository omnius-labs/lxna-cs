using Omnius.Core;
using Omnius.Lxna.Ui.Desktop.Shared;
using Reactive.Bindings;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public abstract class MainWindowModelBase : AsyncDisposableBase
{
    public MainWindowStatus? Status { get; protected set; }
    public ExplorerViewModelBase? ExplorerViewModel { get; protected set; }
    public AsyncReactiveCommand? SettingsCommand { get; protected set; }
}

public class MainWindowModel : MainWindowModelBase
{
    public MainWindowModel(UiStatus uiStatus, ExplorerViewModel ExplorerViewModel)
    {
        this.Status = uiStatus.MainWindow ??= new MainWindowStatus();
        this.ExplorerViewModel = ExplorerViewModel;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        if (this.ExplorerViewModel is not null) await this.ExplorerViewModel.DisposeAsync();
    }
}
