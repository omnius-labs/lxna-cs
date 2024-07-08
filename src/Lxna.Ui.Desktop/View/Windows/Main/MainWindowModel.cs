using Omnius.Core.Base;
using Omnius.Lxna.Ui.Desktop.Shared;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public abstract class MainWindowModelBase : AsyncDisposableBase
{
    public MainWindowStatus? Status { get; protected set; }
    public ExplorerViewModelBase? ExplorerViewModel { get; protected set; }
    public AsyncReactiveCommand? SettingsCommand { get; protected set; }
}

public class MainWindowModel : MainWindowModelBase
{
    private readonly IDialogService _dialogService;

    private readonly CompositeDisposable _disposable = new();

    public MainWindowModel(UiStatus uiStatus, ExplorerViewModel ExplorerViewModel, IDialogService dialogService)
    {
        _dialogService = dialogService;

        this.Status = uiStatus.MainWindow ??= new MainWindowStatus();

        this.ExplorerViewModel = ExplorerViewModel;

        this.SettingsCommand = new AsyncReactiveCommand().AddTo(_disposable);
        this.SettingsCommand.Subscribe(this.ShowSettingsWindow).AddTo(_disposable);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();

        if (this.ExplorerViewModel is not null) await this.ExplorerViewModel.DisposeAsync();
    }

    private async Task ShowSettingsWindow()
    {
        await _dialogService.ShowSettingsWindowAsync();
    }
}
