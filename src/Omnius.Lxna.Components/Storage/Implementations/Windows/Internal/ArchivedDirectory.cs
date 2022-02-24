using System.Runtime.CompilerServices;
using Omnius.Core;
using Omnius.Lxna.Components.Storage.Models;
using Omnius.Lxna.Components.Storage.Windows.Internal.Helpers;

namespace Omnius.Lxna.Components.Storage.Windows.Internal;

internal sealed class ArchivedDirectory : IDirectory
{
    private readonly string _relativePath;
    private readonly ArchivedFileExtractor _extractor;
    private readonly string _tempPath;
    private readonly IBytesPool _bytesPool;

    internal ArchivedDirectory(NestedPath logicalPath, ArchivedFileExtractor extractor, string tempPath, IBytesPool bytesPool)
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
            yield return new ArchivedDirectory(NestedPath.Combine(this.LogicalPath, name), _extractor, _tempPath, _bytesPool);
        }
    }

    public async IAsyncEnumerable<IFile> FindFilesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var name in await _extractor.FindFilesAsync(_relativePath, cancellationToken))
        {
            yield return new ArchivedFile(NestedPath.Combine(this.LogicalPath, name), _extractor, _tempPath, _bytesPool);
        }
    }
}
