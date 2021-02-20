using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Lxna.Ui.Desktop.Resources;

namespace Omnius.Lxna.Ui.Desktop.Views.Windows.Main
{
    public class MainWindowModel : AsyncDisposableBase
    {
        private readonly AppState _state;

        public MainWindowModel(AppState state)
        {
            _state = state;
        }

        protected override async ValueTask OnDisposeAsync()
        {
        }
    }
}
