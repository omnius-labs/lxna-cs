using System.Buffers;
using System.Runtime.InteropServices;
using Omnius.Core;
using Omnius.Lxna.Components.Internal.Helpers;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components;

public sealed partial class FileSystem : AsyncDisposableBase, IFileSystem
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
    private static readonly HashSet<string> _archiveFileExtensionList = new() { ".zip", ".rar", ".7z" };

    private readonly IArchiveFileExtractorProvider _archiveFileExtractorProvider;
    private readonly string _tempDirPath;
    private readonly IBytesPool _bytesPool;

    private readonly ExtractedFileCollector _extractedFileCollector;

    private Task _watchTask = null!;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly AsyncLock _asyncLock = new();

    public static async ValueTask<IFileSystem> CreateAsync(FileSystemOptions options)
    {
        var result = new FileSystem(options);
        await result.InitAsync();

        return result;
    }

    internal FileSystem(FileSystemOptions options)
    {
        _archiveFileExtractorProvider = options.ArchiveFileExtractorProvider ?? throw new ArgumentException($"{nameof(options.ArchiveFileExtractorProvider)} is null");
        _tempDirPath = options.TemporaryDirectoryPath ?? Path.Combine(Path.GetTempPath(), "FileSystem");
        _bytesPool = options.BytesPool ?? BytesPool.Shared;

        _extractedFileCollector = new ExtractedFileCollector(_archiveFileExtractorProvider, _tempDirPath);
    }

    internal async ValueTask InitAsync()
    {
        _watchTask = this.WatchAsync(_cancellationTokenSource.Token);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _cancellationTokenSource.Cancel();
        await _watchTask;
        _cancellationTokenSource.Dispose();

        _extractedFileCollector.Dispose();
        _archiveFileExtractorProvider.Dispose();
    }

    private async Task WatchAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            for (; ; )
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    await _archiveFileExtractorProvider.ShrinkAsync(cancellationToken);
                    await _extractedFileCollector.ShrinkAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException e)
        {
            _logger.Info(e);
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
    }

    public async ValueTask<bool> ExistsFileAsync(NestedPath path, CancellationToken cancellationToken = default)
    {
        if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (path.Values.Count == 1)
            {
                var result = await this.ExistsPhysicalFileAsync(path.Values[0], cancellationToken);
                return result;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await _archiveFileExtractorProvider.CreateAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.ExistsFileAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }
    }

    private async ValueTask<bool> ExistsPhysicalFileAsync(string path, CancellationToken cancellationToken = default)
    {
        return File.Exists(path);
    }

    public async ValueTask<bool> ExistsDirectoryAsync(NestedPath path, CancellationToken cancellationToken = default)
    {
        if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (path.Values.Count == 1)
            {
                var result = await this.ExistsPhysicalDirectoryAsync(path.Values[0], cancellationToken);
                return result;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await _archiveFileExtractorProvider.CreateAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.ExistsDirectoryAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }
    }

    private async ValueTask<bool> ExistsPhysicalDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        return Directory.Exists(path);
    }

    public async ValueTask<DateTime> GetFileLastWriteTimeAsync(NestedPath path, CancellationToken cancellationToken = default)
    {
        if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (path.Values.Count == 1)
            {
                var result = await this.GetPhysicalFileLastWriteTimeAsync(path.Values[0], cancellationToken);
                return result;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await _archiveFileExtractorProvider.CreateAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.GetFileLastWriteTimeAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }
    }

    private async ValueTask<DateTime> GetPhysicalFileLastWriteTimeAsync(string path, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(path);
        return fileInfo.LastAccessTimeUtc;
    }

    public async ValueTask<IEnumerable<NestedPath>> FindDirectoriesAsync(NestedPath? path = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (path is null || path.Values.Count == 0)
            {
                var result = await this.FindRootPhysicalDirectoriesAsync(cancellationToken);
                var list = result.Select(n => new NestedPath(new[] { n })).ToList();
                list.Sort();
                return list;
            }
            else if (path.Values.Count == 1)
            {
                var result = await this.FindPhysicalDirectoriesAsync(path.Values[0], cancellationToken);
                var list = result.Select(n => new NestedPath(new[] { n })).ToList();
                list.Sort();
                return list;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await _archiveFileExtractorProvider.CreateAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.FindDirectoriesAsync(path.Values[^1], cancellationToken);
                var list = result.Select(n => new NestedPath(archiveFilePath.Values.Append(n).ToArray())).ToList();
                list.Sort();
                return list;
            }
        }
    }

    private async ValueTask<IEnumerable<string>> FindRootPhysicalDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Directory.GetLogicalDrives().ToArray();
        }
        else
        {
            return new string[] { "/" };
        }
    }

    private async ValueTask<IEnumerable<string>> FindPhysicalDirectoriesAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetDirectories(path).Select(n => PathHelper.Normalize(n)).ToList();
    }

    public async ValueTask<IEnumerable<NestedPath>> FindArchiveFilesAsync(NestedPath path, CancellationToken cancellationToken = default)
    {
        if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (path.Values.Count == 1)
            {
                var result = await this.FindPhysicalArchiveFilesAsync(path.Values[0], cancellationToken);
                var list = result.Select(n => new NestedPath(new[] { n, "" })).ToList();
                list.Sort();
                return list;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await _archiveFileExtractorProvider.CreateAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.FindArchiveFilesAsync(path.Values[^1], cancellationToken);
                var list = result.Select(n => new NestedPath(archiveFilePath.Values.Append(n).Append("").ToArray())).ToList();
                list.Sort();
                return list;
            }
        }
    }

    private async ValueTask<IEnumerable<string>> FindPhysicalArchiveFilesAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetFiles(path).Select(n => PathHelper.Normalize(n)).Where(n => _archiveFileExtensionList.Contains(Path.GetExtension(n))).ToList();
    }

    public async ValueTask<IEnumerable<NestedPath>> FindFilesAsync(NestedPath path, CancellationToken cancellationToken = default)
    {
        if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (path.Values.Count == 1)
            {
                var result = await this.FindPhysicalFileAsync(path.Values[0], cancellationToken);
                var list = result.Select(n => new NestedPath(new[] { n })).ToList();
                list.Sort();
                return list;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await _archiveFileExtractorProvider.CreateAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.FindFilesAsync(path.Values[^1], cancellationToken);
                var list = result.Select(n => new NestedPath(archiveFilePath.Values.Append(n).ToArray())).ToList();
                list.Sort();
                return list;
            }
        }
    }

    private async ValueTask<IEnumerable<string>> FindPhysicalFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.GetFiles(path).Select(n => PathHelper.Normalize(n)).ToList();
    }

    public async ValueTask<Stream> GetFileStreamAsync(NestedPath path, CancellationToken cancellationToken = default)
    {
        if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (path.Values.Count == 1)
            {
                return await this.GetPhysicalFileStreamAsync(path.Values[0], cancellationToken);
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await _archiveFileExtractorProvider.CreateAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.GetFileStreamAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }
    }

    private async ValueTask<Stream> GetPhysicalFileStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        var fileStream = new FileStream(path, FileMode.Open);
        return fileStream;
    }

    public async ValueTask<long> GetFileSizeAsync(NestedPath path, CancellationToken cancellationToken = default)
    {
        if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (path.Values.Count == 1)
            {
                return await this.GetPhysicalFileSizeAsync(path.Values[0], cancellationToken);
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await _archiveFileExtractorProvider.CreateAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.GetFileSizeAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }
    }

    private async ValueTask<long> GetPhysicalFileSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        return new FileInfo(path).Length;
    }

    public async ValueTask<IExtractedFileOwner?> TryExtractFileAsync(NestedPath path, CancellationToken cancellationToken = default)
    {
        if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

        if (path.Values.Count == 1) return null;

        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            return await _extractedFileCollector.GetExtractedFileAsync(path, cancellationToken);
        }
    }
}
