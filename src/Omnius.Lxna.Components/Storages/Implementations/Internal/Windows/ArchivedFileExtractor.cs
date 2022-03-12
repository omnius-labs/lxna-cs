using Omnius.Core;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Storages.Internal.Windows.Helpers;

namespace Omnius.Lxna.Components.Storages.Internal.Windows;

internal sealed partial class ArchivedFileExtractor : DisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private static readonly HashSet<string> _archiveFileExtensionList = new() { ".zip", ".rar", ".7z" };

    private readonly string _archiveFilePath;
    private readonly IBytesPool _bytesPool;

    private SevenZipExtractor.ArchiveFile _archiveFile = null!;
    private readonly Dictionary<string, SevenZipExtractor.Entry> _fileEntryMap = new();
    private readonly HashSet<string> _dirSet = new();

    private readonly Random _random = new();

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

        _archiveFile = new SevenZipExtractor.ArchiveFile(_archiveFilePath);
        this.ComputeFilesAndDirs(cancellationToken);
    }

    private void ComputeFilesAndDirs(CancellationToken cancellationToken = default)
    {
        foreach (var entry in _archiveFile.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.IsFolder)
            {
                var dirPath = PathHelper.Normalize(entry.FileName);
                _dirSet.Add(dirPath);
            }
            else
            {
                var filePath = PathHelper.Normalize(entry.FileName);
                _fileEntryMap.Add(filePath, entry);
            }
        }

        foreach (var filePath in _fileEntryMap.Keys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var dirPath in PathHelper.ExtractDirectories(filePath))
            {
                _dirSet.Add(dirPath);
            }
        }
    }

    protected override void OnDispose(bool disposing)
    {
        _archiveFile.Dispose();
        _fileEntryMap.Clear();
        _dirSet.Clear();
    }

    public static bool IsSupported(string path)
    {
        return _archiveFileExtensionList.Contains(Path.GetExtension(path));
    }

    public async ValueTask<bool> ExistsFileAsync(string path, CancellationToken cancellationToken = default)
    {
        return _fileEntryMap.ContainsKey(path);
    }

    public async ValueTask<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        return _dirSet.Contains(path);
    }

    public async ValueTask<DateTime> GetFileLastWriteTimeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_fileEntryMap.TryGetValue(path, out var entry))
        {
            return entry.LastWriteTime.ToUniversalTime();
        }

        throw new FileNotFoundException();
    }

    public async ValueTask<IEnumerable<string>> FindDirectoriesAsync(string path, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();

        foreach (var dirPath in _dirSet)
        {
            if (PathHelper.IsCurrentDirectory(path, dirPath))
            {
                results.Add(Path.GetFileName(dirPath));
            }
        }

        return results;
    }

    public async ValueTask<IEnumerable<string>> FindFilesAsync(string path, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();

        foreach (var filePath in _fileEntryMap.Keys)
        {
            if (PathHelper.IsCurrentDirectory(path, filePath))
            {
                results.Add(Path.GetFileName(filePath));
            }
        }

        return results;
    }

    public async ValueTask<Stream> GetFileStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_fileEntryMap.TryGetValue(path, out var entry))
        {
            var memoryStream = new RecyclableMemoryStream(_bytesPool);
            entry.Extract(memoryStream);
            memoryStream.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        throw new FileNotFoundException();
    }

    public async ValueTask<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_fileEntryMap.TryGetValue(path, out var entry))
        {
            return (long)entry.Size;
        }

        throw new FileNotFoundException();
    }

    public async ValueTask ExtractFileAsync(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        if (_fileEntryMap.TryGetValue(path, out var entry))
        {
            entry.Extract(stream);
            stream.Flush();
            return;
        }

        throw new FileNotFoundException();
    }
}
