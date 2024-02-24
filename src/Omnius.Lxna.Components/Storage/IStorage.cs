namespace Omnius.Lxna.Components.Storage;

public interface IStorage
{
    ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default);
}
