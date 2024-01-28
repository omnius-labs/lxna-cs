using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Omnius.Axis.Ui.Desktop.Windows.Dialogs.PicturePreview;
using Omnius.Core.Avalonia;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Ui.Desktop.Configuration;

namespace Omnius.Lxna.Ui.Desktop.Internal;

public interface IDialogService
{
    ValueTask<IEnumerable<string>> ShowOpenFileWindowAsync(CancellationToken cancellationToken = default);
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
            var window = new PicturePreviewWindow(Path.Combine(_lxnaEnvironment.DatabaseDirectoryPath, "windows", "picture_preview"));
            var serviceProvider = Bootstrapper.Instance.GetServiceProvider();

            var viewModel = serviceProvider.GetRequiredService<PicturePreviewWindowModel>();
            await viewModel.InitializeAsync(file, cancellationToken);
            window.DataContext = viewModel;

            await window.ShowDialog(_mainWindowProvider.GetMainWindow());
        });
    }
}
