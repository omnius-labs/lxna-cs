using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Omnius.Lxna.Ui.Desktop.Windows.Views.Primitives
{
    public abstract class StatefulWindowBase : Window
    {
        private bool _isDisposing = false;
        private bool _isDisposed = false;

        public StatefulWindowBase()
        {
            this.Opened += (_, _) => this.OnOpened();
        }

        private async void OnOpened()
        {
            await this.OnInitialize();
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (_isDisposed)
            {
                return;
            }

            e.Cancel = true;

            if (_isDisposing)
            {
                return;
            }

            _isDisposing = true;

            await this.OnDispose();

            _isDisposed = true;

            this.Close();
        }

        protected abstract ValueTask OnInitialize();

        protected abstract ValueTask OnDispose();
    }
}
