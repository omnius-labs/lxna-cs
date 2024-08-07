using Omnius.Core.Base;
using Omnius.Lxna.Components.Storage.Internal.Helpers;

namespace Omnius.Lxna.Components.Storage.Internal;

internal sealed class ArchivedDirectory : IDirectory
{
    private readonly string _relativePath;
    private readonly Func<CancellationToken, ValueTask<ArchivedFileExtractor>> _createExtractor;
    private readonly string _tempPath;
    private readonly IBytesPool _bytesPool;

    internal ArchivedDirectory(IBytesPool bytesPool, Func<CancellationToken, ValueTask<ArchivedFileExtractor>> createExtractor, NestedPath logicalPath, string tempPath)
    {
        _relativePath = logicalPath.GetLastPath();
        _createExtractor = createExtractor;
        _tempPath = tempPath;
        _bytesPool = bytesPool;

        this.LogicalNestedPath = logicalPath;
        this.Name = this.LogicalNestedPath.GetName();
    }

    public void Dispose()
    {
    }

    public string Name { get; }
    public NestedPath LogicalNestedPath { get; }
    public DirectoryAttributes Attributes => DirectoryAttributes.Archive;
    public bool IsReadOnly => throw new NotImplementedException();
    public bool Exists => throw new NotImplementedException();

    public ValueTask<string> GetPhysicalPathAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<DateTime> GetCreationTimeAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<DateTime> GetLastAccessTimeAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<DateTime> GetLastWriteTimeAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public ValueTask<bool> TryMoveToAsync(NestedPath path, bool overwrite, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<bool> TryCopyToAsync(NestedPath path, bool overwrite, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<bool> TryDeleteAsync(bool recursive, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public async ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var extractor = await _createExtractor.Invoke(cancellationToken);

        var results = new List<IDirectory>();

        foreach (var name in await extractor.FindDirectoriesAsync(_relativePath, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(new ArchivedDirectory(_bytesPool, _createExtractor, NestedPath.Combine(this.LogicalNestedPath, name), _tempPath));
        }

        return results;
    }

    public async ValueTask<IEnumerable<IFile>> FindFilesAsync(CancellationToken cancellationToken = default)
    {
        var extractor = await _createExtractor.Invoke(cancellationToken);

        var results = new List<IFile>();

        foreach (var name in await extractor.FindFilesAsync(_relativePath, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(new ArchivedFile(_bytesPool, extractor, NestedPath.Combine(this.LogicalNestedPath, name), _tempPath));
        }

        return results;
    }

    public async ValueTask<bool> ExistsFileAsync(string name, CancellationToken cancellationToken = default)
    {
        var extractor = await _createExtractor.Invoke(cancellationToken);
        return await extractor.ExistsFileAsync(PathHelper.Combine(_relativePath, name), cancellationToken);
    }

    public async ValueTask<bool> ExistsDirectoryAsync(string name, CancellationToken cancellationToken = default)
    {
        var extractor = await _createExtractor.Invoke(cancellationToken);
        return await extractor.ExistsDirectoryAsync(PathHelper.Combine(_relativePath, name), cancellationToken);
    }

    public ValueTask<bool> TryCreateDirectoryAsync(string name, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<bool> TryCreateFileAsync(string name, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<bool> TryDeleteDirectoryAsync(string name, bool recursive, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public ValueTask<bool> TryDeleteFileAsync(string name, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
