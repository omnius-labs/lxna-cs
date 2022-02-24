using Omnius.Core;
using Omnius.Lxna.Components.Storage.Models;

namespace Omnius.Lxna.Components.Storage.Windows.Internal;

internal sealed class LocalFile : IFile
{
    private readonly string _physicalPath;
    private readonly string _tempPath;
    private readonly IBytesPool _bytesPool;

    private ArchivedFileExtractor? _extractedFileExtractor;

    public LocalFile(string physicalPath, string tempPath, IBytesPool bytesPool)
    {
        _physicalPath = physicalPath;
        _tempPath = tempPath;
        _bytesPool = bytesPool;

        this.LogicalPath = new NestedPath(_physicalPath);
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

    public async ValueTask<IDirectory?> TryConvertToDirectory(CancellationToken cancellationToken = default)
    {
        if (!ArchivedFileExtractor.IsSupported(_physicalPath)) return null;

        _extractedFileExtractor ??= await ArchivedFileExtractor.CreateAsync(_physicalPath, _bytesPool, cancellationToken);
        return new ArchivedDirectory(NestedPath.Union(this.LogicalPath, new NestedPath("")), _extractedFileExtractor, _tempPath, _bytesPool);
    }
}
