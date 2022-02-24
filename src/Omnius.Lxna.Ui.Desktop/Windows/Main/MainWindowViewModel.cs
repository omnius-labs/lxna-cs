using Omnius.Core;
using Omnius.Lxna.Ui.Desktop.Configuration;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public abstract class MainWindowViewModelBase : AsyncDisposableBase
{
    public MainWindowStatus? Status { get; protected set; }

    public FileViewControlViewModelBase? FileViewControlViewModel { get; protected set; }
}

public class MainWindowViewModel : MainWindowViewModelBase
{
    private readonly UiStatus _uiStatus;

    private readonly CompositeDisposable _disposable = new();

    public MainWindowViewModel(UiStatus uiState, FileViewControlViewModel fileViewControlViewModel)
    {
        _uiStatus = uiState;
        this.FileViewControlViewModel = fileViewControlViewModel;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }
}
