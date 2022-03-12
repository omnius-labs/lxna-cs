using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Omnius.Axis.Ui.Desktop.Windows.PicturePreview;
using Omnius.Core.Avalonia;
using Omnius.Lxna.Components.Storages;

namespace Omnius.Lxna.Ui.Desktop.Internal;

public interface IDialogService
{
    ValueTask<IEnumerable<string>> ShowOpenFileWindowAsync(CancellationToken cancellationToken = default);

    ValueTask ShowPicturePreviewWindowAsync(IFile file, CancellationToken cancellationToken = default);
}

public class DialogService : IDialogService
{
    private readonly IApplicationDispatcher _applicationDispatcher;
    private readonly IMainWindowProvider _mainWindowProvider;
    private readonly IClipboardService _clipboardService;

    public DialogService(IApplicationDispatcher applicationDispatcher, IMainWindowProvider mainWindowProvider, IClipboardService clipboardService)
    {
        _applicationDispatcher = applicationDispatcher;
        _mainWindowProvider = mainWindowProvider;
        _clipboardService = clipboardService;
    }

    public async ValueTask<IEnumerable<string>> ShowOpenFileWindowAsync(CancellationToken cancellationToken = default)
    {
        return await _applicationDispatcher.InvokeAsync(async () =>
        {
            var dialog = new OpenFileDialog();
            dialog.AllowMultiple = true;

            return await dialog.ShowAsync(_mainWindowProvider.GetMainWindow());
        }) ?? Enumerable.Empty<string>();
    }

    public async ValueTask ShowPicturePreviewWindowAsync(IFile file, CancellationToken cancellationToken = default)
    {
        await _applicationDispatcher.InvokeAsync(async () =>
        {
            var window = new PicturePreviewWindow();
            var serviceProvider = Bootstrapper.Instance.GetServiceProvider();

            var viewModel = serviceProvider.GetRequiredService<PicturePreviewWindowModel>();
            await viewModel.InitializeAsync(file, cancellationToken);
            window.ViewModel = viewModel;

            await window.ShowDialog(_mainWindowProvider.GetMainWindow());
        });
    }
}
