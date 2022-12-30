using Avalonia.Controls;
using Omnius.Lxna.Ui.Desktop.Internal;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.Windows.Settings;

public abstract class SettingsWindowModelBase : AsyncDisposableBase
{
    public abstract ValueTask InitializeAsync(CancellationToken cancellationToken = default);

    public SettingsWindowStatus? Status { get; protected set; }
    public AsyncReactiveCommand? OkCommand { get; protected set; }
    public AsyncReactiveCommand? CancelCommand { get; protected set; }
}

public class SettingsWindowModel : SettingsWindowModelBase
{
    private readonly UiStatus _uiState;
    private readonly IDialogService _dialogService;

    private readonly CompositeDisposable _disposable = new();

    public SettingsWindowModel(UiStatus uiState, IDialogService dialogService)
    {
        _uiState = uiState;
        _dialogService = dialogService;

        this.Status = _uiState.SettingsWindow ??= new SettingsWindowStatus();

        var serviceProvider = Bootstrapper.Instance.GetServiceProvider();

        this.OkCommand = new AsyncReactiveCommand().AddTo(_disposable);
        this.OkCommand.Subscribe(async (state) => await this.OkAsync(state)).AddTo(_disposable);
        this.CancelCommand = new AsyncReactiveCommand().AddTo(_disposable);
        this.CancelCommand.Subscribe(async (state) => await this.CancelAsync(state)).AddTo(_disposable);
    }

    public override async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        await this.LoadAsync();
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }

    private async Task OkAsync(object state)
    {
        var window = (Window)state;
        window.Close();

        await this.SaveAsync();
    }

    private async Task CancelAsync(object state)
    {
        var window = (Window)state;
        window.Close();
    }

    private async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
    }

    private async ValueTask SaveAsync(CancellationToken cancellationToken = default)
    {
    }
}
