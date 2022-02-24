using Microsoft.Extensions.DependencyInjection;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Storage.Windows;
using Omnius.Lxna.Components.Thumbnail;
using Omnius.Lxna.Components.Thumbnail.Models;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Lxna.Ui.Desktop.Interactors.Internal;
using Omnius.Lxna.Ui.Desktop.Windows.Main;

namespace Omnius.Lxna.Ui.Desktop.Internal;

public partial class Bootstrapper : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private string? _databaseDirectoryPath;

    private UiStatus? _uiState;
    private IThumbnailGenerator? _thumbnailGenerator;

    private ServiceProvider? _serviceProvider;

    public static Bootstrapper Instance { get; } = new Bootstrapper();

    private const string UI_STATE_FILE_NAME = "ui_status.json";

    private Bootstrapper()
    {
    }

    public async ValueTask BuildAsync(string databaseDirectoryPath, CancellationToken cancellationToken = default)
    {
        _databaseDirectoryPath = databaseDirectoryPath;

        ArgumentNullException.ThrowIfNull(_databaseDirectoryPath);

        try
        {
            _uiState = await UiStatus.LoadAsync(Path.Combine(_databaseDirectoryPath, UI_STATE_FILE_NAME));
            _thumbnailGenerator = await ThumbnailGenerator.CreateAsync(BytesPool.Shared, new ThumbnailGeneratorOptions(Path.Combine(databaseDirectoryPath, "thumbnail_generator"), 12));

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(_uiState);
            serviceCollection.AddSingleton<IThumbnailGenerator>(_thumbnailGenerator);
            serviceCollection.AddSingleton<IThumbnailsViewer, ThumbnailsViewer>();
            serviceCollection.AddSingleton<IStorage, Storage>();
            serviceCollection.AddSingleton<IBytesPool>(BytesPool.Shared);

            serviceCollection.AddSingleton<IApplicationDispatcher, ApplicationDispatcher>();
            serviceCollection.AddSingleton<IMainWindowProvider, MainWindowProvider>();
            serviceCollection.AddSingleton<IClipboardService, ClipboardService>();
            serviceCollection.AddSingleton<IDialogService, DialogService>();

            serviceCollection.AddTransient<MainWindowViewModel>();
            serviceCollection.AddTransient<FileViewControlViewModel>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }
        catch (OperationCanceledException e)
        {
            _logger.Debug(e);

            throw;
        }
        catch (Exception e)
        {
            _logger.Error(e);

            throw;
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        if (_databaseDirectoryPath is not null && _uiState is not null) await _uiState.SaveAsync(Path.Combine(_databaseDirectoryPath, UI_STATE_FILE_NAME));
    }

    public ServiceProvider GetServiceProvider()
    {
        return _serviceProvider ?? throw new NullReferenceException();
    }
}
