using Omnius.Core;
using Omnius.Lxna.Components.Storages.Internal;
using Omnius.Lxna.Components.Storages.Internal.Helpers;

namespace Omnius.Lxna.Components.Storages;

public sealed class LxnaStorage : IStorage
{
    private readonly string _tempPath;
    private readonly IBytesPool _bytesPool;

    public LxnaStorage(IBytesPool bytesPool, string tempPath)
    {
        _tempPath = tempPath;
        _bytesPool = bytesPool;
    }

    public static async ValueTask<IStorage> CreateAsync(IBytesPool bytesPool, LxnaStorageOptions options, CancellationToken cancellationToken = default)
    {
        return new LxnaStorage(bytesPool, options.TempDirectoryPath);
    }

    public async ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IDirectory>();

        foreach (var drive in Directory.GetLogicalDrives())
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(new LocalDirectory(_bytesPool, PathHelper.Normalize(drive), _tempPath));
        }

        return results;
    }
}
