namespace Omnius.Lxna.Components.Storages;

public interface IStorage
{
    IAsyncEnumerable<IDirectory> FindDirectoriesAsync(CancellationToken cancellationToken = default);
}
