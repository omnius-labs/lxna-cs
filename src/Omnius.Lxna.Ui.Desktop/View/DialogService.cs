using Microsoft.Extensions.DependencyInjection;
using Omnius.Lxna.Ui.Desktop.View.Windows;
using Omnius.Core.Avalonia;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Ui.Desktop.Shared;

namespace Omnius.Lxna.Ui.Desktop.View;

public interface IDialogService
{
    ValueTask ShowPicturePreviewWindowAsync(IFile file, CancellationToken cancellationToken = default);
}

public class DialogService : IDialogService
{
    private readonly LxnaEnvironment _lxnaEnvironment;
    private readonly IApplicationDispatcher _applicationDispatcher;
    private readonly IMainWindowProvider _mainWindowProvider;
    private readonly IClipboardService _clipboardService;

    public DialogService(LxnaEnvironment lxnaEnvironment, IApplicationDispatcher applicationDispatcher, IMainWindowProvider mainWindowProvider, IClipboardService clipboardService)
    {
        _lxnaEnvironment = lxnaEnvironment;
        _applicationDispatcher = applicationDispatcher;
        _mainWindowProvider = mainWindowProvider;
        _clipboardService = clipboardService;
    }

    public async ValueTask ShowPicturePreviewWindowAsync(IFile file, CancellationToken cancellationToken = default)
    {
        await _applicationDispatcher.InvokeAsync(async () =>
        {
            var window = new PicturePreviewWindow(Path.Combine(_lxnaEnvironment.StateDirectoryPath, "windows", "picture_preview"));
            var serviceProvider = Bootstrapper.Instance.GetServiceProvider();

            var viewModel = serviceProvider.GetRequiredService<PicturePreviewWindowModel>();
            await viewModel.InitializeAsync(file, cancellationToken);
            window.DataContext = viewModel;

            await window.ShowDialog(_mainWindowProvider.GetMainWindow());
        });
    }
}
