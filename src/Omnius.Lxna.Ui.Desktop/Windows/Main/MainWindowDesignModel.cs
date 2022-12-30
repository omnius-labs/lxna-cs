using Omnius.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Lxna.Ui.Desktop.Windows.Settings;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public class MainWindowDesignModel : MainWindowModelBase
{
    private readonly CompositeDisposable _disposable = new();

    public MainWindowDesignModel()
    {
        this.Status = new MainWindowStatus();

        this.ExplorerViewModel = new ExplorerViewDesignModel();

        this.SettingsCommand = new AsyncReactiveCommand().AddTo(_disposable);
        this.SettingsCommand.Subscribe(async () => await this.SettingsAsync()).AddTo(_disposable);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }

    private async Task SettingsAsync()
    {
        var window = new SettingsWindow();
        window.ViewModel = new SettingsWindowDesignModel();
        await window.ShowDialog(App.Current!.MainWindow);
    }
}
