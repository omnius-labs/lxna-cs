using Omnius.Lxna.Components.Storage.Models;
using Omnius.Core;
using Omnius.Lxna.Components.Storage.Windows.Internal.Helpers;

namespace Omnius.Lxna.Components.Storage.Windows.Internal;

public sealed class ArchivedFile : IFile
{
    private readonly string _relativePath;
    private readonly ArchivedFileExtractor _extractor;
    private readonly string _tempPath;
    private readonly IBytesPool _bytesPool;

    private ArchivedFileExtractor? _extractedFileExtractor;
    private FileStream? _fileStream;

    private readonly AsyncLock _asyncLock = new();

    internal ArchivedFile(NestedPath logicalPath, ArchivedFileExtractor extractor, string tempPath, IBytesPool bytesPool)
    {
        _relativePath = logicalPath.GetLastPath();
        _extractor = extractor;
        _tempPath = tempPath;
        _bytesPool = bytesPool;

        this.LogicalPath = logicalPath;
        this.Name = this.LogicalPath.GetName();
    }

    public void Dispose()
    {
    }

    public string Name { get; }

    public NestedPath LogicalPath { get; }

    public FileAttributes Attributes
    {
        get
        {
            if (ArchivedFileExtractor.IsSupported(_relativePath)) return FileAttributes.Archive;
            return FileAttributes.Normal;
        }
    }

    public async ValueTask<long> GetLengthAsync(CancellationToken cancellationToken = default)
    {
        return await _extractor.GetFileSizeAsync(_relativePath, cancellationToken);
    }

    public async ValueTask<DateTime> GetLastWriteTimeAsync(CancellationToken cancellationToken = default)
    {
        return await _extractor.GetFileLastWriteTimeAsync(_relativePath, cancellationToken);
    }

    public async ValueTask<Stream> GetStreamAsync(CancellationToken cancellationToken = default)
    {
        return await _extractor.GetFileStreamAsync(_relativePath, cancellationToken);
    }

    public async ValueTask<string> GetPhysicalPathAsync(CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (_fileStream is not null) return _fileStream.Name;

            _fileStream = FileHelper.GenTempFileStream(_tempPath, Path.GetExtension(_relativePath));
            await _extractor.ExtractFileAsync(_relativePath, _fileStream, cancellationToken);
            return _fileStream.Name;
        }
    }

    public async ValueTask<IDirectory?> TryConvertDirectory(CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (!ArchivedFileExtractor.IsSupported(_relativePath)) return null;

            var physicalPath = await this.GetPhysicalPathAsync(cancellationToken);
            _extractedFileExtractor ??= await ArchivedFileExtractor.CreateAsync(physicalPath, _bytesPool, cancellationToken);
            return new ArchivedDirectory(NestedPath.Union(this.LogicalPath, new NestedPath("")), _extractedFileExtractor, _tempPath, _bytesPool);
        }
    }

    public ValueTask<IDirectory?> TryConvertToDirectory(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
