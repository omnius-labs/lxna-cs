using Omnius.Core;
using Omnius.Lxna.Components.Storage.Internal.Helpers;

namespace Omnius.Lxna.Components.Storage.Internal;

public sealed class ArchivedFile : IFile
{
    private readonly string _relativePath;
    private readonly ArchivedFileExtractor _extractor;
    private readonly string _tempPath;
    private readonly IBytesPool _bytesPool;

    private ArchivedFileExtractor? _archivedFileExtractor;
    private string? _extractedFilePath;

    private readonly AsyncLock _asyncLock = new();

    internal ArchivedFile(IBytesPool bytesPool, ArchivedFileExtractor extractor, NestedPath logicalPath, string tempPath)
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
        _archivedFileExtractor?.Dispose();
        if (_extractedFilePath is not null) File.Delete(_extractedFilePath);
    }

    public string Name { get; }
    public string Extension => throw new NotImplementedException();
    public NestedPath LogicalPath { get; }
    public FileAttributes Attributes
    {
        get
        {
            if (ArchivedFileExtractor.IsSupported(_relativePath)) return FileAttributes.Archive;
            return FileAttributes.Normal;
        }
    }
    public bool IsReadOnly => throw new NotImplementedException();
    public bool Exists => throw new NotImplementedException();

    public async ValueTask<string> GetPhysicalPathAsync(CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (_extractedFilePath is not null) return _extractedFilePath;

            using var fileStream = FileHelper.GenTempFileStream(_tempPath, Path.GetExtension(_relativePath));
            await _extractor.ExtractFileAsync(_relativePath, fileStream, cancellationToken);
            _extractedFilePath = fileStream.Name;

            return _extractedFilePath;
        }
    }

    public ValueTask<DateTime> GetCreationTimeAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<DateTime> GetLastAccessTimeAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public async ValueTask<DateTime> GetLastWriteTimeAsync(CancellationToken cancellationToken = default)
    {
        return await _extractor.GetFileLastWriteTimeAsync(_relativePath, cancellationToken);
    }

    public async ValueTask<long> GetLengthAsync(CancellationToken cancellationToken = default)
    {
        return await _extractor.GetFileSizeAsync(_relativePath, cancellationToken);
    }

    public async ValueTask<Stream> GetStreamAsync(CancellationToken cancellationToken = default)
    {
        return await _extractor.GetFileStreamAsync(_relativePath, cancellationToken);
    }

    public async ValueTask<IDirectory?> TryConvertToDirectoryAsync(CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (!ArchivedFileExtractor.IsSupported(_relativePath)) return null;

            return new ArchivedDirectory(_bytesPool, this.InternalCreateExtractor, NestedPath.Union(this.LogicalPath, new NestedPath("")), _tempPath);
        }
    }

    private async ValueTask<ArchivedFileExtractor> InternalCreateExtractor(CancellationToken cancellationToken)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            var physicalPath = await this.GetPhysicalPathAsync(cancellationToken);
            return _archivedFileExtractor ??= await ArchivedFileExtractor.CreateAsync(_bytesPool, physicalPath, cancellationToken);
        }
    }

    public ValueTask<bool> TryMoveToAsync(NestedPath path, bool overwrite, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<bool> TryCopyToAsync(NestedPath path, bool overwrite, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<bool> TryDeleteAsync(bool recursive, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
