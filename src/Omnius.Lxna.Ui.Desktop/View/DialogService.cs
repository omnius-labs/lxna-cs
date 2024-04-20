using Microsoft.Extensions.DependencyInjection;
using Omnius.Core.Avalonia;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Ui.Desktop.Shared;
using Omnius.Lxna.Ui.Desktop.View.Windows;

namespace Omnius.Lxna.Ui.Desktop.View;

public interface IDialogService
{
    ValueTask ShowPreviewWindowAsync(IEnumerable<IFile> files, int position, CancellationToken cancellationToken = default);
    ValueTask ShowSettingsWindowAsync(CancellationToken cancellationToken = default);
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

    public async ValueTask ShowPreviewWindowAsync(IEnumerable<IFile> files, int position, CancellationToken cancellationToken = default)
    {
        await _applicationDispatcher.InvokeAsync(async () =>
        {
            var window = new PreviewWindow(Path.Combine(_lxnaEnvironment.StateDirectoryPath, "windows", "preview"));
            var serviceProvider = Bootstrapper.Instance.GetServiceProvider();

            var viewModel = serviceProvider.GetRequiredService<PreviewWindowModel>();
            await viewModel.InitializeAsync(files, position, cancellationToken);
            window.DataContext = viewModel;

            await window.ShowDialog(_mainWindowProvider.GetMainWindow());
        });
    }

    public async ValueTask ShowSettingsWindowAsync(CancellationToken cancellationToken = default)
    {
        await _applicationDispatcher.InvokeAsync(async () =>
        {
            var window = new SettingsWindow(Path.Combine(_lxnaEnvironment.StateDirectoryPath, "windows", "settings"));
            var serviceProvider = Bootstrapper.Instance.GetServiceProvider();

            var viewModel = serviceProvider.GetRequiredService<SettingsWindowModel>();
            await viewModel.InitializeAsync(cancellationToken);
            window.DataContext = viewModel;

            await window.ShowDialog(_mainWindowProvider.GetMainWindow());
        });
    }
}
