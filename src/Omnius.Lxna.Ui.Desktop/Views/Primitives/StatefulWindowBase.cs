using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Omnius.Lxna.Ui.Desktop.Views.Primitives
{
    public abstract class StatefulWindowBase : Window
    {
        private bool _isInitialized = false;
        private bool _isDisposing = false;
        private bool _isDisposed = false;

        public StatefulWindowBase()
        {
            this.Activated += (_, _) => this.OnActivated();
        }

        private async void OnActivated()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            await this.OnInitialize();
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (_isDisposed) return;

            e.Cancel = true;

            if (_isDisposing) return;

            _isDisposing = true;

            await this.OnDispose();

            _isDisposed = true;

            this.Close();
        }

        protected abstract ValueTask OnInitialize();

        protected abstract ValueTask OnDispose();
    }
}
