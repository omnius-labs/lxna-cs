using Omnius.Lxna.Components.Storage.Models;

namespace Omnius.Lxna.Components.Storage;

public interface IDirectory : IDisposable
{
    public string Name { get; }

    public NestedPath LogicalPath { get; }

    ValueTask<bool> ExistsFileAsync(string name, CancellationToken cancellationToken = default);

    ValueTask<bool> ExistsDirectoryAsync(string name, CancellationToken cancellationToken = default);

    IAsyncEnumerable<IDirectory> FindDirectoriesAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<IFile> FindFilesAsync(CancellationToken cancellationToken = default);
}
