namespace Omnius.Lxna.Components.Storages;

public interface IStorage
{
    ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default);
}
