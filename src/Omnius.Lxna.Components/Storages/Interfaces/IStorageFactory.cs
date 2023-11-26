using Omnius.Core;

namespace Omnius.Lxna.Components.Storages;

public interface IStorageFactory
{
    ValueTask<IStorage> CreateAsync(IBytesPool bytesPool, LocalStorageOptions options, CancellationToken cancellationToken = default);
}
