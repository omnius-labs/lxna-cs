using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Omnius.Core;
using Omnius.Core.Helpers;
using Omnius.Lxna.Components;
using Omnius.Lxna.Ui.Desktop.Resources.Models;

namespace Omnius.Lxna.Ui.Desktop.Resources
{
    public class AppState : AsyncDisposableBase
    {
        private readonly string _configDirectoryPath;
        private readonly string _temporaryDirectoryPath;
        private readonly IBytesPool _bytesPool;

        private UiSettings _uiSettings = null!;
        private IFileSystem _fileSystem = null!;
        private IThumbnailGenerator _thumbnailGenerator = null!;

        private readonly AsyncReaderWriterLock _asyncLock = new();

        private const string UiSettingsFilePath = "omnius.lxna.ui.desktop/ui_settings/config.json";
        private const string ThumbnailGeneratorDirPath = "omnius.lxna.components/thumbnail_generator";

        public class AppStateFactory
        {
            public async ValueTask<AppState> CreateAsync(string configDirectoryPath, string temporaryDirectoryPath, IBytesPool bytesPool)
            {
                var result = new AppState(configDirectoryPath, temporaryDirectoryPath, bytesPool);
                await result.InitAsync();

                return result;
            }
        }

        public static AppStateFactory Factory { get; } = new();

        private AppState(string configDirectoryPath, string temporaryDirectoryPath, IBytesPool bytesPool)
        {
            _configDirectoryPath = configDirectoryPath;
            _temporaryDirectoryPath = temporaryDirectoryPath;
            _bytesPool = bytesPool;
        }

        private async ValueTask InitAsync()
        {
            var configPath = _configDirectoryPath;
            DirectoryHelper.CreateDirectory(configPath);

            var tempPath = _temporaryDirectoryPath;
            DirectoryHelper.CreateDirectory(tempPath);

            _uiSettings = await CreateUiSettings(configPath);
            _fileSystem = await CreateFileSystem(tempPath, _bytesPool);
            _thumbnailGenerator = await CreateThumbnailGenerator(configPath, _fileSystem);
        }

        private static async ValueTask<UiSettings> CreateUiSettings(string configPath, CancellationToken cancellationToken = default)
        {
            var uiSettings = await UiSettings.LoadAsync(Path.Combine(configPath, UiSettingsFilePath));

            if (uiSettings is null)
            {
                uiSettings = new UiSettings
                {
                    Thumbnail_Width = 256,
                    Thumbnail_Height = 256,
                    FileView_TreeViewWidth = 200,
                };
            }

            return uiSettings;
        }

        private static async ValueTask<IFileSystem> CreateFileSystem(string tempPath, IBytesPool bytesPool)
        {
            var fileSystemOptions = new FileSystemOptions()
            {
                ArchiveFileExtractorFactory = ArchiveFileExtractor.Factory,
                TemporaryDirectoryPath = tempPath,
                BytesPool = bytesPool,
            };
            var fileSystem = await FileSystem.Factory.CreateAsync(fileSystemOptions);
            return fileSystem;
        }

        private static async ValueTask<IThumbnailGenerator> CreateThumbnailGenerator(string configPath, IFileSystem fileSystem)
        {
            var thumbnailGeneratorOptions = new ThumbnailGeneratorOptions()
            {
                ConfigPath = Path.Combine(configPath, ThumbnailGeneratorDirPath),
                Concurrency = 8,
                FileSystem = fileSystem,
            };
            var thumbnailGenerator = await ThumbnailGenerator.Factory.CreateAsync(thumbnailGeneratorOptions);
            return thumbnailGenerator;
        }

        protected override async ValueTask OnDisposeAsync()
        {
            await this.SaveAsync();

            await _thumbnailGenerator.DisposeAsync();
            await _fileSystem.DisposeAsync();
        }

        private async ValueTask SaveAsync()
        {
            var configPath = _configDirectoryPath;
            DirectoryHelper.CreateDirectory(configPath);

            await _uiSettings.SaveAsync(Path.Combine(configPath, UiSettingsFilePath));
        }

        public IBytesPool GetBytesPool() => _bytesPool;

        public UiSettings GetUiSettings() => _uiSettings;

        public IFileSystem GetFileSystem() => _fileSystem;

        public IThumbnailGenerator GetThumbnailGenerator() => _thumbnailGenerator;
    }
}
