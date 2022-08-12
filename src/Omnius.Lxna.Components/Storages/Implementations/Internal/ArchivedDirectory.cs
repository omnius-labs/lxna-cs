using Omnius.Core;
using Omnius.Lxna.Components.Storages.Internal.Helpers;
using Omnius.Lxna.Components.Storages.Models;

namespace Omnius.Lxna.Components.Storages.Internal;

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

        this.LogicalPath = logicalPath;
        this.Name = this.LogicalPath.GetName();
    }

    public void Dispose()
    {
    }

    public string Name { get; }

    public NestedPath LogicalPath { get; }

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

    public async ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var extractor = await _createExtractor.Invoke(cancellationToken);

        var results = new List<IDirectory>();

        foreach (var name in await extractor.FindDirectoriesAsync(_relativePath, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(new ArchivedDirectory(_bytesPool, _createExtractor, NestedPath.Combine(this.LogicalPath, name), _tempPath));
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
            results.Add(new ArchivedFile(_bytesPool, extractor, NestedPath.Combine(this.LogicalPath, name), _tempPath));
        }

        return results;
    }
}
