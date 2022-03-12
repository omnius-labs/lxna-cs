using Omnius.Core;

namespace Omnius.Lxna.Components.Storages;

public sealed class WindowsStorageFactory : IStorageFactory
{
    private readonly IBytesPool _bytesPool;
    private readonly WindowsStorageFactoryOptions _options;

    public WindowsStorageFactory(IBytesPool bytesPool, WindowsStorageFactoryOptions options)
    {
        _bytesPool = bytesPool;
        _options = options;
    }

    public async ValueTask<IStorage> CreateAsync(CancellationToken cancellationToken = default)
    {
        return new WindowsStorage(_bytesPool, _options.TempDirectoryPath);
    }
}
