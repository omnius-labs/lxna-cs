using Avalonia.Controls;
using Omnius.Core;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.Windows.Settings;

public class SettingsWindowDesignModel : SettingsWindowModelBase
{
    private readonly CompositeDisposable _disposable = new();

    public SettingsWindowDesignModel()
    {
        this.Status = new SettingsWindowStatus();

        this.OkCommand = new AsyncReactiveCommand().AddTo(_disposable);
        this.OkCommand.Subscribe(async (state) => await this.OkAsync(state)).AddTo(_disposable);

        this.CancelCommand = new AsyncReactiveCommand().AddTo(_disposable);
        this.CancelCommand.Subscribe(async (state) => await this.CancelAsync(state)).AddTo(_disposable);
    }

    public override async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }

    private async Task OkAsync(object state)
    {
        var window = (Window)state;
        window.Close();
    }

    private async Task CancelAsync(object state)
    {
        var window = (Window)state;
        window.Close();
    }
}