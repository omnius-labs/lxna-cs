using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Lxna.Components;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Lxna.Ui.Desktop.Windows;
using Omnius.Lxna.Ui.Desktop.Windows.Main;
using Omnius.Lxna.Ui.Desktop.Windows.Main.FileView;
using Omnius.Lxna.Ui.Desktop.Windows.PicturePreview;

namespace Omnius.Lxna.Ui.Desktop
{
    public static class Bootstrapper
    {
        public static ServiceProvider? ServiceProvider { get; private set; }

        public static async ValueTask RegisterAsync(string stateDirectoryPath, string temporaryDirectoryPath, CancellationToken cancellationToken = default)
        {
            var serviceCollection = new ServiceCollection();

            var bytesPool = BytesPool.Shared;
            serviceCollection.AddSingleton<IBytesPool>(bytesPool);

            var uiState = await LoadUiStateAsync(stateDirectoryPath, cancellationToken);
            serviceCollection.AddSingleton(uiState);

            var fileSystem = await CreateFileSystem(temporaryDirectoryPath, bytesPool, cancellationToken);
            serviceCollection.AddSingleton(fileSystem);

            var thumbnailGenerator = await CreateThumbnailGenerator(stateDirectoryPath, fileSystem, cancellationToken);
            serviceCollection.AddSingleton(thumbnailGenerator);

            serviceCollection.AddSingleton<IApplicationDispatcher, ApplicationDispatcher>();
            serviceCollection.AddSingleton<IMainWindowProvider, MainWindowProvider>();
            serviceCollection.AddSingleton<IDialogService, DialogService>();

            serviceCollection.AddTransient<MainWindowViewModel>();
            serviceCollection.AddTransient<FileViewControlViewModel>();
            serviceCollection.AddTransient<PicturePreviewWindowViewModel>();

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private static async ValueTask<UiState> LoadUiStateAsync(string stateDirectoryPath, CancellationToken cancellationToken = default)
        {
            var filePath = Path.Combine(stateDirectoryPath, "ui_state.json");
            var uiState = await UiState.LoadAsync(filePath);

            if (uiState is null)
            {
                uiState = new UiState
                {
                    Thumbnail_Width = 256,
                    Thumbnail_Height = 256,
                    FileView_TreeViewWidth = 200,
                };

                await uiState.SaveAsync(filePath);
            }

            return uiState;
        }

        private static async ValueTask<IFileSystem> CreateFileSystem(string temporaryDirectoryPath, IBytesPool bytesPool, CancellationToken cancellationToken = default)
        {
            var fileSystemOptions = new FileSystemOptions()
            {
                ArchiveFileExtractorFactory = ArchiveFileExtractor.Factory,
                TemporaryDirectoryPath = temporaryDirectoryPath,
                BytesPool = bytesPool,
            };
            var fileSystem = await FileSystem.Factory.CreateAsync(fileSystemOptions);
            return fileSystem;
        }

        private static async ValueTask<IThumbnailGenerator> CreateThumbnailGenerator(string stateDirectoryPath, IFileSystem fileSystem, CancellationToken cancellationToken = default)
        {
            var thumbnailGeneratorOptions = new ThumbnailGeneratorOptions()
            {
                ConfigDirectoryPath = Path.Combine(stateDirectoryPath, "omnius.lxna.components/thumbnail_generator"),
                Concurrency = 8,
                FileSystem = fileSystem,
            };
            var thumbnailGenerator = await ThumbnailGenerator.Factory.CreateAsync(thumbnailGeneratorOptions);
            return thumbnailGenerator;
        }
    }
}
