using Omnius.Core;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.ThumbnailGenerators.Models;
using Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common;

namespace Omnius.Lxna.Components.ThumbnailGenerators;

internal sealed class WindowsThumbnailGenerator : AsyncDisposableBase, IThumbnailGenerator
{
    private readonly IBytesPool _bytesPool;
    private readonly string _configDirectoryPath;
    private readonly int _concurrency;

    private FilePictureThumbnailGenerator? _filePictureThumbnailGenerator;
    private FileMovieThumbnailGenerator? _fileMovieThumbnailGenerator;
    private DirectoryThumbnailGenerator? _directoryThumbnailGenerator;

    public static async ValueTask<IThumbnailGenerator> CreateAsync(IBytesPool bytesPool, string configDirectoryPath, int concurrency, CancellationToken cancellationToken = default)
    {
        var result = new WindowsThumbnailGenerator(bytesPool, configDirectoryPath, concurrency);
        await result.InitAsync(cancellationToken);

        return result;
    }

    private WindowsThumbnailGenerator(IBytesPool bytesPool, string configDirectoryPath, int concurrency)
    {
        _bytesPool = bytesPool;
        _configDirectoryPath = configDirectoryPath;
        _concurrency = concurrency;
    }

    internal async ValueTask InitAsync(CancellationToken cancellationToken = default)
    {
        _filePictureThumbnailGenerator = await FilePictureThumbnailGenerator.CreateAsync(_bytesPool, Path.Combine(_configDirectoryPath, "file_picture"), _concurrency, cancellationToken);
        _fileMovieThumbnailGenerator = await FileMovieThumbnailGenerator.CreateAsync(_bytesPool, Path.Combine(_configDirectoryPath, "file_movie"), _concurrency, cancellationToken);
        _directoryThumbnailGenerator = await DirectoryThumbnailGenerator.CreateAsync(_bytesPool, Path.Combine(_configDirectoryPath, "directory"), _concurrency, cancellationToken);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        if (_filePictureThumbnailGenerator is not null) _filePictureThumbnailGenerator?.DisposeAsync();
        if (_fileMovieThumbnailGenerator is not null) _fileMovieThumbnailGenerator?.DisposeAsync();
    }

    public async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(IFile file, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default)
    {
        var result = await _filePictureThumbnailGenerator!.GetThumbnailAsync(file, options, cacheOnly, cancellationToken);
        if (result.Status == ThumbnailGeneratorGetThumbnailResultStatus.Succeeded) return result;

        return await _fileMovieThumbnailGenerator!.GetThumbnailAsync(file, options, cacheOnly, cancellationToken);
    }

    public async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(IDirectory directory, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default)
    {
        return await _directoryThumbnailGenerator!.GetThumbnailAsync(directory, options, cacheOnly, cancellationToken);
    }
}
