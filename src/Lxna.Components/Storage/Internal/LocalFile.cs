using Core.Base;

namespace Lxna.Components.Storage.Internal;

internal sealed class LocalFile : IFile
{
    private readonly IBytesPool _bytesPool;
    private readonly string _physicalPath;
    private readonly string _tempPath;

    private ArchivedFileExtractor? _archivedFileExtractor;

    public LocalFile(IBytesPool bytesPool, string physicalPath, string tempPath)
    {
        _bytesPool = bytesPool;
        _physicalPath = physicalPath;
        _tempPath = tempPath;

        this.LogicalPath = new NestedPath(_physicalPath);
        this.Name = this.LogicalPath.GetName();
        this.Extension = Path.GetExtension(this.Name);
    }

    public void Dispose()
    {
        _archivedFileExtractor?.Dispose();
    }

    public string Name { get; }
    public string Extension { get; }
    public NestedPath LogicalPath { get; }

    public FileAttributes Attributes
    {
        get
        {
            if (ArchivedFileExtractor.IsSupported(_physicalPath)) return FileAttributes.Archive;
            return FileAttributes.Normal;
        }
    }

    public bool IsReadOnly => false;
    public bool IsArchive => ArchivedFileExtractor.IsSupported(_physicalPath);
    public bool Exists => File.Exists(_physicalPath);

    public async ValueTask<string> GetPhysicalPathAsync(CancellationToken cancellationToken = default)
    {
        return _physicalPath;
    }

    public ValueTask<DateTime> GetCreationTimeAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<DateTime> GetLastAccessTimeAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public async ValueTask<DateTime> GetLastWriteTimeAsync(CancellationToken cancellationToken = default)
    {
        var info = new FileInfo(_physicalPath);
        return info.LastWriteTimeUtc;
    }

    public async ValueTask<long> GetLengthAsync(CancellationToken cancellationToken = default)
    {
        var info = new FileInfo(_physicalPath);
        return info.Length;
    }

    public async ValueTask<Stream> GetStreamAsync(CancellationToken cancellationToken = default)
    {
        return new FileStream(_physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.None);
    }

    public async ValueTask<IDirectory?> TryConvertToDirectoryAsync(CancellationToken cancellationToken = default)
    {
        if (!ArchivedFileExtractor.IsSupported(_physicalPath)) return null;

        return new ArchivedDirectory(_bytesPool, this.InternalCreateExtractor, NestedPath.Union(this.LogicalPath, new NestedPath("")), _tempPath);
    }

    private async ValueTask<ArchivedFileExtractor> InternalCreateExtractor(CancellationToken cancellationToken)
    {
        return _archivedFileExtractor ??= await ArchivedFileExtractor.CreateAsync(_bytesPool, _physicalPath, cancellationToken);
    }

    public ValueTask<bool> TryMoveToAsync(NestedPath path, bool overwrite, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<bool> TryCopyToAsync(NestedPath path, bool overwrite, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<bool> TryDeleteAsync(bool recursive, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
