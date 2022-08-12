using Omnius.Core;
using Omnius.Lxna.Components.Storages.Models;

namespace Omnius.Lxna.Components.Storages.Internal;

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
    }

    public void Dispose()
    {
        _archivedFileExtractor?.Dispose();
    }

    public string Name { get; }

    public NestedPath LogicalPath { get; }

    public FileAttributes Attributes
    {
        get
        {
            if (ArchivedFileExtractor.IsSupported(_physicalPath)) return FileAttributes.Archive;
            return FileAttributes.Normal;
        }
    }

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
        return new FileStream(_physicalPath, FileMode.Open);
    }

    public async ValueTask<string> GetPhysicalPathAsync(CancellationToken cancellationToken = default)
    {
        return _physicalPath;
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
}
