using System.Reactive.Disposables;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Lxna.Ui.Desktop.Windows.Main.FileView;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main
{
    public class MainWindowViewModel : AsyncDisposableBase
    {
        private readonly UiState _uiState;

        private readonly CompositeDisposable _disposable = new();

        public MainWindowViewModel(UiState uiState, FileViewControlViewModel fileViewControlViewModel)
        {
            _uiState = uiState;
            this.FileViewControlViewModel = fileViewControlViewModel;
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _disposable.Dispose();
        }

        public FileViewControlViewModel FileViewControlViewModel { get; }
    }
}
