using Microsoft.Extensions.DependencyInjection;
using Omnius.Axis.Ui.Desktop.Windows.Dialogs.PicturePreview;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Helpers;
using Omnius.Lxna.Components.IconGenerators;
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

            var storageOptions = new LocalStorageOptions { TempDirectoryPath = tempDirectoryPath };
            var storage = await LocalStorage.CreateAsync(bytesPool, storageOptions, cancellationToken);

            var directoryIconGeneratorOptions = new DirectoryIconGeneratorOptions
            {
                ConfigDirectoryPath = Path.Combine(_lxnaEnvironment.DatabaseDirectoryPath, "directory_icon_generator"),
                Concurrency = Math.Max(2, Environment.ProcessorCount / 2),
            };
            var directoryIconGenerator = await DirectoryIconGenerator.CreateAsync(bytesPool, directoryIconGeneratorOptions, cancellationToken);

            var fileThumbnailGeneratorOptions = new FileThumbnailGeneratorOptions
            {
                ConfigDirectoryPath = Path.Combine(_lxnaEnvironment.DatabaseDirectoryPath, "file_thumbnail_generator"),
                Concurrency = Math.Max(2, Environment.ProcessorCount / 2),
            };
            var fileThumbnailGenerator = await FileThumbnailGenerator.CreateAsync(bytesPool, fileThumbnailGeneratorOptions, cancellationToken);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(_lxnaEnvironment);
            serviceCollection.AddSingleton<IBytesPool>(bytesPool);
            serviceCollection.AddSingleton(uiStatus);
            serviceCollection.AddSingleton<IStorage>(storage);
            serviceCollection.AddSingleton(directoryIconGenerator);
            serviceCollection.AddSingleton(fileThumbnailGenerator);
            serviceCollection.AddSingleton<ThumbnailsViewer>();

            serviceCollection.AddSingleton<IApplicationDispatcher, ApplicationDispatcher>();
            serviceCollection.AddSingleton<IMainWindowProvider, MainWindowProvider>();
            serviceCollection.AddSingleton<IClipboardService, ClipboardService>();
            serviceCollection.AddSingleton<IDialogService, DialogService>();

            serviceCollection.AddTransient<MainWindowModel>();
            serviceCollection.AddTransient<ExplorerViewModel>();
            serviceCollection.AddTransient<PicturePreviewWindowModel>();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }
        catch (OperationCanceledException e)
        {
            _logger.Debug(e, "Operation Canceled");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unexpected Exception");

            throw;
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        if (_lxnaEnvironment is null) return;
        if (_serviceProvider is null) return;

        var uiStatus = _serviceProvider.GetRequiredService<UiStatus>();
        await uiStatus.SaveAsync(Path.Combine(_lxnaEnvironment.DatabaseDirectoryPath, UI_STATUS_FILE_NAME));

        await _serviceProvider.GetRequiredService<DirectoryIconGenerator>().DisposeAsync();
        await _serviceProvider.GetRequiredService<FileThumbnailGenerator>().DisposeAsync();
        await _serviceProvider.GetRequiredService<ThumbnailsViewer>().DisposeAsync();
    }

    public ServiceProvider GetServiceProvider()
    {
        return _serviceProvider ?? throw new NullReferenceException();
    }
}
