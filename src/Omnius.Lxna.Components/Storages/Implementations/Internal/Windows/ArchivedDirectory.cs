using System.Runtime.CompilerServices;
using Omnius.Core;
using Omnius.Lxna.Components.Storages.Models;
using Omnius.Lxna.Components.Storages.Internal.Windows.Helpers;

namespace Omnius.Lxna.Components.Storages.Internal.Windows;

internal sealed class ArchivedDirectory : IDirectory
{
    private readonly string _relativePath;
    private readonly ArchivedFileExtractor _extractor;
    private readonly string _tempPath;
    private readonly IBytesPool _bytesPool;

    internal ArchivedDirectory(IBytesPool bytesPool, ArchivedFileExtractor extractor, NestedPath logicalPath, string tempPath)
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

    public async ValueTask<bool> ExistsFileAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _extractor.ExistsFileAsync(PathHelper.Combine(_relativePath, name), cancellationToken);
    }

    public async ValueTask<bool> ExistsDirectoryAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _extractor.ExistsDirectoryAsync(PathHelper.Combine(_relativePath, name), cancellationToken);
    }

    public async IAsyncEnumerable<IDirectory> FindDirectoriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var name in await _extractor.FindDirectoriesAsync(_relativePath, cancellationToken))
        {
            yield return new ArchivedDirectory(_bytesPool, _extractor, NestedPath.Combine(this.LogicalPath, name), _tempPath);
        }
    }

    public async IAsyncEnumerable<IFile> FindFilesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var name in await _extractor.FindFilesAsync(_relativePath, cancellationToken))
        {
            yield return new ArchivedFile(_bytesPool, _extractor, NestedPath.Combine(this.LogicalPath, name), _tempPath);
        }
    }
}
