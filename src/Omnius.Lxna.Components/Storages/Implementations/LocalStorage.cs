using Omnius.Core;
using Omnius.Lxna.Components.Storages.Internal;
using Omnius.Lxna.Components.Storages.Internal.Helpers;

namespace Omnius.Lxna.Components.Storages;

public sealed class LocalStorage : IStorage
{
    private readonly LocalStorageOptions _options;
    private readonly IBytesPool _bytesPool;

    private LocalStorage(IBytesPool bytesPool, LocalStorageOptions options)
    {
        _options = options;
        _bytesPool = bytesPool;
    }

    public static async ValueTask<IStorageController> CreateAsync(IBytesPool bytesPool, LocalStorageOptions options, CancellationToken cancellationToken = default)
    {
        return new LocalStorage(bytesPool, options);
    }

    public async ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IDirectory>();

        foreach (var dir in Directory.GetDirectories("/"))
        {
            results.Add(new LocalDirectory(_bytesPool, PathHelper.Normalize(_options.RootDirectoryPath), _options.TempDirectoryPath));
        }

        return results;
    }
}
