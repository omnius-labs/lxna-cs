using Omnius.Lxna.Components.Storages.Models;

namespace Omnius.Lxna.Components.Storages;

public interface IDirectory : IDisposable
{
    public string Name { get; }

    public NestedPath LogicalPath { get; }

    ValueTask<bool> ExistsFileAsync(string name, CancellationToken cancellationToken = default);

    ValueTask<bool> ExistsDirectoryAsync(string name, CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<IFile>> FindFilesAsync(CancellationToken cancellationToken = default);
}
