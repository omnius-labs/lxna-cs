using Microsoft.Extensions.DependencyInjection;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Helpers;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.ThumbnailGenerators;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Lxna.Ui.Desktop.Interactors.Internal;
using Omnius.Lxna.Ui.Desktop.Windows.Main;

namespace Omnius.Lxna.Ui.Desktop.Internal;

public partial class Bootstrapper : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private string? _databaseDirectoryPath;

    private UiStatus? _uiStatus;
    private IThumbnailGenerator? _thumbnailGenerator;
    private IStorage? _storage;

    private ServiceProvider? _serviceProvider;

    public static Bootstrapper Instance { get; } = new Bootstrapper();

    private const string UI_STATUS_FILE_NAME = "ui_status.json";

    private Bootstrapper()
    {
    }

    public async ValueTask BuildAsync(string databaseDirectoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseDirectoryPath);

        _databaseDirectoryPath = databaseDirectoryPath;

        var tempDirectoryPath = Path.Combine(databaseDirectoryPath, "temp");
        if (Directory.Exists(tempDirectoryPath)) Directory.Delete(tempDirectoryPath, true);
        DirectoryHelper.CreateDirectory(tempDirectoryPath);

        try
        {
            _uiStatus = await UiStatus.LoadAsync(Path.Combine(_databaseDirectoryPath, UI_STATUS_FILE_NAME));

            var bytesPool = BytesPool.Shared;

            var storageFactoryOptions = new WindowsStorageFactoryOptions(tempDirectoryPath);
            var storageFactory = new WindowsStorageFactory(bytesPool, storageFactoryOptions);
            _storage = await storageFactory.CreateAsync(cancellationToken);

            var thumbnailGeneratorFactoryOptions = new WindowsThumbnailGeneratorFatcoryOptions(Path.Combine(databaseDirectoryPath, "thumbnail_generator"), 12);
            var thumbnailGeneratorFactory = new WindowsThumbnailGeneratorFactory(bytesPool, thumbnailGeneratorFactoryOptions);
            _thumbnailGenerator = await thumbnailGeneratorFactory.CreateAsync(cancellationToken);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(_uiStatus);

            serviceCollection.AddSingleton<IBytesPool>(bytesPool);
            serviceCollection.AddSingleton<IStorage>(_storage);
            serviceCollection.AddSingleton<IThumbnailGenerator>(_thumbnailGenerator);

            serviceCollection.AddSingleton<IThumbnailsViewer, ThumbnailsViewer>();

            serviceCollection.AddSingleton<IApplicationDispatcher, ApplicationDispatcher>();
            serviceCollection.AddSingleton<IMainWindowProvider, MainWindowProvider>();
            serviceCollection.AddSingleton<IClipboardService, ClipboardService>();
            serviceCollection.AddSingleton<IDialogService, DialogService>();

            serviceCollection.AddTransient<MainWindowModel>();
            serviceCollection.AddTransient<FileExplorerViewModel>();

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
        if (_databaseDirectoryPath is not null && _uiStatus is not null) await _uiStatus.SaveAsync(Path.Combine(_databaseDirectoryPath, UI_STATUS_FILE_NAME));
    }

    public ServiceProvider GetServiceProvider()
    {
        return _serviceProvider ?? throw new NullReferenceException();
    }
}
