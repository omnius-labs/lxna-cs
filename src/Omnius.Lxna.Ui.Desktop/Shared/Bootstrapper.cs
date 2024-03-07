using Microsoft.Extensions.DependencyInjection;
using Omnius.Lxna.Ui.Desktop.View.Windows;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Thumbnail;
using Omnius.Lxna.Ui.Desktop.Service.Thumbnail;
using Omnius.Lxna.Ui.Desktop.View;

namespace Omnius.Lxna.Ui.Desktop.Shared;

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

        try
        {
            var bytesPool = BytesPool.Shared;

            var uiStatus = await UiStatus.LoadAsync(Path.Combine(_lxnaEnvironment.StateDirectoryPath, UI_STATUS_FILE_NAME));

            var imageConverter = await ImageConverter.CreateAsync(bytesPool, cancellationToken);

            var directoryThumbnailGenerator = new DirectoryThumbnailGenerator(imageConverter, bytesPool);

            var fileThumbnailGeneratorOptions = new FileThumbnailGeneratorOptions
            {
                StateDirectoryPath = Path.Combine(_lxnaEnvironment.StateDirectoryPath, "file_thumbnail_generator"),
                Concurrency = Math.Max(2, Environment.ProcessorCount / 2),
            };
            var fileThumbnailGenerator = await FileThumbnailGenerator.CreateAsync(imageConverter, bytesPool, fileThumbnailGeneratorOptions, cancellationToken);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(_lxnaEnvironment);
            serviceCollection.AddSingleton<IBytesPool>(bytesPool);
            serviceCollection.AddSingleton(uiStatus);
            serviceCollection.AddSingleton(imageConverter);
            serviceCollection.AddSingleton(directoryThumbnailGenerator);
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
        await uiStatus.SaveAsync(Path.Combine(_lxnaEnvironment.StateDirectoryPath, UI_STATUS_FILE_NAME));

        await _serviceProvider.GetRequiredService<FileThumbnailGenerator>().DisposeAsync();
        await _serviceProvider.GetRequiredService<ThumbnailsViewer>().DisposeAsync();
    }

    public ServiceProvider GetServiceProvider()
    {
        return _serviceProvider ?? throw new NullReferenceException();
    }
}
