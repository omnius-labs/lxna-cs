using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Lxna.Components;
using Omnius.Lxna.Ui.Desktop.Resources.Models;

namespace Omnius.Lxna.Ui.Desktop.Resources
{
    public class AppState : AsyncDisposableBase
    {
        private readonly string _stateDirectoryPath;
        private readonly string _temporaryDirectoryPath;
        private readonly IBytesPool _bytesPool;

        private UiSettings _uiSettings = null!;
        private IFileSystem _fileSystem = null!;
        private IThumbnailGenerator _thumbnailGenerator = null!;

        public class AppStateFactory
        {
            public async ValueTask<AppState> CreateAsync(string stateDirectoryPath, string temporaryDirectoryPath, IBytesPool bytesPool)
            {
                var result = new AppState(stateDirectoryPath, temporaryDirectoryPath, bytesPool);
                await result.InitAsync();

                return result;
            }
        }

        public static AppStateFactory Factory { get; } = new();

        private AppState(string stateDirectoryPath, string temporaryDirectoryPath, IBytesPool bytesPool)
        {
            _stateDirectoryPath = stateDirectoryPath;
            _temporaryDirectoryPath = temporaryDirectoryPath;
            _bytesPool = bytesPool;
        }

        private string GetUiSettingsFilePath() => Path.Combine(_stateDirectoryPath, "ui_settings.json");

        private async ValueTask InitAsync()
        {
            _fileSystem = await this.CreateFileSystem(_bytesPool);

            _thumbnailGenerator = await this.CreateThumbnailGenerator(_fileSystem);

            _uiSettings = await this.CreateUiSettings();
        }

        protected override async ValueTask OnDisposeAsync()
        {
            await this.SaveAsync();

            await _thumbnailGenerator.DisposeAsync();
            await _fileSystem.DisposeAsync();
        }

        private async ValueTask SaveAsync()
        {
            await _uiSettings.SaveAsync(this.GetUiSettingsFilePath());
        }

        private async ValueTask<UiSettings> CreateUiSettings(CancellationToken cancellationToken = default)
        {
            var uiSettings = await UiSettings.LoadAsync(this.GetUiSettingsFilePath());

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

        private async ValueTask<IFileSystem> CreateFileSystem(IBytesPool bytesPool)
        {
            var fileSystemOptions = new FileSystemOptions()
            {
                ArchiveFileExtractorFactory = ArchiveFileExtractor.Factory,
                TemporaryDirectoryPath = _temporaryDirectoryPath,
                BytesPool = bytesPool,
            };
            var fileSystem = await FileSystem.Factory.CreateAsync(fileSystemOptions);
            return fileSystem;
        }

        private async ValueTask<IThumbnailGenerator> CreateThumbnailGenerator(IFileSystem fileSystem)
        {
            var thumbnailGeneratorOptions = new ThumbnailGeneratorOptions()
            {
                ConfigDirectoryPath = Path.Combine(_stateDirectoryPath, "omnius.lxna.components/thumbnail_generator"),
                Concurrency = 8,
                FileSystem = fileSystem,
            };
            var thumbnailGenerator = await ThumbnailGenerator.Factory.CreateAsync(thumbnailGeneratorOptions);
            return thumbnailGenerator;
        }

        public IBytesPool GetBytesPool() => _bytesPool;

        public UiSettings GetUiSettings() => _uiSettings;

        public IFileSystem GetFileSystem() => _fileSystem;

        public IThumbnailGenerator GetThumbnailGenerator() => _thumbnailGenerator;
    }
}
