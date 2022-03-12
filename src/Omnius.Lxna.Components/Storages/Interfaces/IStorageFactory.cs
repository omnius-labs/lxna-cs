namespace Omnius.Lxna.Components.Storages;

public interface IStorageFactory
{
    ValueTask<IStorage> CreateAsync(CancellationToken cancellationToken = default);
}
