using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Omnius.Core.Base;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Storage.Internal.Helpers;
using SharpCompress.Archives;

namespace Omnius.Lxna.Components.Storage.Internal;

internal sealed partial class ArchivedFileExtractor : DisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private static readonly ImmutableHashSet<string> _archiveFileExtensionList = [".zip", ".rar", ".7z"];

    private readonly string _archiveFilePath;
    private readonly IBytesPool _bytesPool;

    private IArchive _archiveFile = null!;
    private ImmutableDictionary<string, IArchiveEntry> _fileEntryMap = ImmutableDictionary<string, IArchiveEntry>.Empty;
    private ImmutableHashSet<string> _dirSet = ImmutableHashSet<string>.Empty;

    private readonly AsyncLock _asyncLock = new();

    public static async ValueTask<ArchivedFileExtractor> CreateAsync(IBytesPool bytesPool, string path, CancellationToken cancellationToken = default)
    {
        var result = new ArchivedFileExtractor(bytesPool, path);
        await result.InitAsync(cancellationToken);

        return result;
    }

    private ArchivedFileExtractor(IBytesPool bytesPool, string path)
    {
        _bytesPool = bytesPool;
        _archiveFilePath = path;
    }

    private async ValueTask InitAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        _archiveFile = this.CreateArchive();
        this.ComputeFilesAndDirs(cancellationToken);
    }

    private IArchive CreateArchive()
    {
        if (CultureInfo.CurrentCulture.Name == "ja-JP")
        {
            if (Path.GetExtension(_archiveFilePath).IndexOf(".zip", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                var options = new SharpCompress.Readers.ReaderOptions();
                var encoding = Encoding.GetEncoding(932);
                options.ArchiveEncoding = new SharpCompress.Common.ArchiveEncoding
                {
                    CustomDecoder = encoding.GetString
                };
                return SharpCompress.Archives.Zip.ZipArchive.Open(_archiveFilePath, options);
            }
        }

        return ArchiveFactory.Open(_archiveFilePath);
    }

    private void ComputeFilesAndDirs(CancellationToken cancellationToken = default)
    {
        try
        {
            var fileEntryMap = ImmutableDictionary.CreateBuilder<string, IArchiveEntry>();
            var dirSet = ImmutableHashSet.CreateBuilder<string>();

            foreach (var entry in _archiveFile.Entries)
            {
                if (entry is null) continue;

                cancellationToken.ThrowIfCancellationRequested();

                if (entry.IsDirectory)
                {
                    var dirPath = PathHelper.Normalize(entry.Key).TrimEnd('/');
                    dirSet.Add(dirPath);
                }
                else
                {
                    var filePath = PathHelper.Normalize(entry.Key);
                    fileEntryMap.Add(filePath, entry);
                }
            }

            foreach (var filePath in fileEntryMap.Keys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var dirPath in PathHelper.ExtractDirectories(filePath))
                {
                    dirSet.Add(dirPath);
                }
            }

            _fileEntryMap = fileEntryMap.ToImmutable();
            _dirSet = dirSet.ToImmutable();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
    }

    protected override void OnDispose(bool disposing)
    {
        using (_asyncLock.Lock())
        {
            if (disposing)
            {
                _archiveFile.Dispose();
                _fileEntryMap.Clear();
                _dirSet.Clear();
            }
        }
    }

    public static bool IsSupported(string path)
    {
        return _archiveFileExtensionList.Contains(Path.GetExtension(path));
    }

    public async ValueTask<bool> ExistsFileAsync(string path, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            return _fileEntryMap.ContainsKey(path);
        }
    }

    public async ValueTask<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            return _dirSet.Contains(path);
        }
    }

    public async ValueTask<DateTime> GetFileLastWriteTimeAsync(string path, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (_fileEntryMap.TryGetValue(path, out var entry))
            {
                return entry.LastModifiedTime?.ToUniversalTime() ?? DateTime.MinValue;
            }

            throw new FileNotFoundException();
        }
    }

    public async ValueTask<IEnumerable<string>> FindDirectoriesAsync(string path, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            var results = new List<string>();

            foreach (var dirPath in _dirSet)
            {
                if (PathHelper.IsParentDirectory(path, dirPath))
                {
                    results.Add(Path.GetFileName(dirPath));
                }
            }

            return results;
        }
    }

    public async ValueTask<IEnumerable<string>> FindFilesAsync(string path, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            var results = new List<string>();

            foreach (var filePath in _fileEntryMap.Keys)
            {
                if (PathHelper.IsParentDirectory(path, filePath))
                {
                    results.Add(Path.GetFileName(filePath));
                }
            }

            return results;
        }
    }

    public async ValueTask<Stream> GetFileStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (_fileEntryMap.TryGetValue(path, out var entry))
            {
                var memoryStream = new RecyclableMemoryStream(_bytesPool);
                using var neverCloseStream = new NeverCloseStream(memoryStream);
                using var cancellableStream = new CancellableStream(neverCloseStream, cancellationToken);
                entry.WriteTo(cancellableStream);
                cancellableStream.Flush();
                cancellableStream.Seek(0, SeekOrigin.Begin);

                return memoryStream;
            }

            throw new FileNotFoundException();
        }
    }

    public async ValueTask<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (_fileEntryMap.TryGetValue(path, out var entry))
            {
                return entry.Size;
            }

            throw new FileNotFoundException();
        }
    }

    public async ValueTask ExtractFileAsync(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (_fileEntryMap.TryGetValue(path, out var entry))
            {
                using var neverCloseStream = new NeverCloseStream(stream);
                using var cancellableStream = new CancellableStream(neverCloseStream, cancellationToken);
                entry.WriteTo(cancellableStream);
                cancellableStream.Flush();
                return;
            }

            throw new FileNotFoundException();
        }
    }
}
