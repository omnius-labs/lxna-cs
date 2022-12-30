using Omnius.Core;
using Omnius.Lxna.Components.Storages.Internal;
using Omnius.Lxna.Components.Storages.Internal.Helpers;

namespace Omnius.Lxna.Components.Storages;

public sealed class LxnaStorage : IStorage
{
    private readonly LxnaStorageOptions _options;
    private readonly IBytesPool _bytesPool;

    public LxnaStorage(IBytesPool bytesPool, LxnaStorageOptions options)
    {
        _options = options;
        _bytesPool = bytesPool;
    }

    public static async ValueTask<IStorage> CreateAsync(IBytesPool bytesPool, LxnaStorageOptions options, CancellationToken cancellationToken = default)
    {
        return new LxnaStorage(bytesPool, options);
    }

    public async ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IDirectory>();

        if (_options.RootDirectoryPath is string rootDirectoryPath)
        {
            results.Add(new LocalDirectory(_bytesPool, PathHelper.Normalize(rootDirectoryPath), _options.TempDirectoryPath));
        }
        else
        {
            foreach (var drive in Directory.GetLogicalDrives())
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(new LocalDirectory(_bytesPool, PathHelper.Normalize(drive), _options.TempDirectoryPath));
            }
        }

        return results;
    }
}
