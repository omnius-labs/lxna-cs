namespace Omnius.Lxna.Components.Storage;

public interface IStorage
{
    ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(string? rootDirectoryPath = null, CancellationToken cancellationToken = default);
}
