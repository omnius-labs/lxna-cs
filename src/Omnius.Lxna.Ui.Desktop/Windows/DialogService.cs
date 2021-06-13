using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Omnius.Core.Avalonia;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Windows.PicturePreview;

namespace Omnius.Lxna.Ui.Desktop.Windows
{
    public interface IDialogService
    {
        ValueTask OpenPicturePreviewWindowAsync(NestedPath path);
    }

    public class DialogService : IDialogService
    {
        private readonly IMainWindowProvider _mainWindowProvider;

        public DialogService(IMainWindowProvider mainWindowProvider)
        {
            _mainWindowProvider = mainWindowProvider;
        }

        public async ValueTask OpenPicturePreviewWindowAsync(NestedPath path)
        {
            PicturePreviewWindow window = null!;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                window = new PicturePreviewWindow();
            });

            await window.ViewModel.LoadAsync(path);
            await window.ShowDialog(_mainWindowProvider.GetMainWindow());
        }
    }
}
