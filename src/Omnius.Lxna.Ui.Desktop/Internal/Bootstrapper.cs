using Microsoft.Extensions.DependencyInjection;
using Omnius.Axis.Ui.Desktop.Windows.PicturePreview;
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

    private LxnaEnvironment? _lxnaEnvironment;
    private ServiceProvider? _serviceProvider;

    public static Bootstrapper Instance { get; } = new Bootstrapper();

    private const string UI_STATUS_FILE_NAME = "ui_status.json";

    private Bootstrapper()
    {
    }

    public async ValueTask BuildAsync(LxnaEnvironment lxnaEnvironment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lxnaEnvironment);

        _lxnaEnvironment = lxnaEnvironment;

        var tempDirectoryPath = Path.Combine(_lxnaEnvironment.DatabaseDirectoryPath, "temp");
        if (Directory.Exists(tempDirectoryPath)) Directory.Delete(tempDirectoryPath, true);
        DirectoryHelper.CreateDirectory(tempDirectoryPath);

        try
        {
            var bytesPool = BytesPool.Shared;

            var uiStatus = await UiStatus.LoadAsync(Path.Combine(_lxnaEnvironment.DatabaseDirectoryPath, UI_STATUS_FILE_NAME));

            var storageFactoryOptions = new WindowsStorageFactoryOptions(tempDirectoryPath);
            var storageFactory = new WindowsStorageFactory(bytesPool, storageFactoryOptions);
            var storage = await storageFactory.CreateAsync(cancellationToken);

            var thumbnailGeneratorFactoryOptions = new WindowsThumbnailGeneratorFatcoryOptions(Path.Combine(_lxnaEnvironment.DatabaseDirectoryPath, "thumbnail_generator"), Math.Max(2, Environment.ProcessorCount / 2));
            var thumbnailGeneratorFactory = new WindowsThumbnailGeneratorFactory(bytesPool, thumbnailGeneratorFactoryOptions);
            var thumbnailGenerator = await thumbnailGeneratorFactory.CreateAsync(cancellationToken);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IBytesPool>(bytesPool);
            serviceCollection.AddSingleton(uiStatus);
            serviceCollection.AddSingleton<IStorage>(storage);
            serviceCollection.AddSingleton<IThumbnailGenerator>(thumbnailGenerator);
            serviceCollection.AddSingleton<IThumbnailsViewer, ThumbnailsViewer>();

            serviceCollection.AddSingleton<IApplicationDispatcher, ApplicationDispatcher>();
            serviceCollection.AddSingleton<IMainWindowProvider, MainWindowProvider>();
            serviceCollection.AddSingleton<IClipboardService, ClipboardService>();
            serviceCollection.AddSingleton<IDialogService, DialogService>();

            serviceCollection.AddTransient<MainWindowModel>();
            serviceCollection.AddTransient<FileExplorerViewModel>();
            serviceCollection.AddTransient<PicturePreviewWindowModel>();

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
        if (_lxnaEnvironment is null) return;
        if (_serviceProvider is null) return;

        var uiStatus = _serviceProvider.GetRequiredService<UiStatus>();
        await uiStatus.SaveAsync(Path.Combine(_lxnaEnvironment.DatabaseDirectoryPath, UI_STATUS_FILE_NAME));

        await _serviceProvider.GetRequiredService<IThumbnailGenerator>().DisposeAsync();
        await _serviceProvider.GetRequiredService<IThumbnailsViewer>().DisposeAsync();
    }

    public ServiceProvider GetServiceProvider()
    {
        return _serviceProvider ?? throw new NullReferenceException();
    }
}
