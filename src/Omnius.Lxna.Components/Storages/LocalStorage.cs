using System.Runtime.InteropServices;
using Omnius.Core;
using Omnius.Lxna.Components.Storages.Internal;
using Omnius.Lxna.Components.Storages.Internal.Helpers;

namespace Omnius.Lxna.Components.Storages;

public record LocalStorageOptions
{
    public required string TempDirectoryPath { get; init; }
}

public sealed class LocalStorage : IStorage
{
    private readonly LocalStorageOptions _options;
    private readonly IBytesPool _bytesPool;

    private LocalStorage(IBytesPool bytesPool, LocalStorageOptions options)
    {
        _options = options;
        _bytesPool = bytesPool;
    }

    public static async ValueTask<LocalStorage> CreateAsync(IBytesPool bytesPool, LocalStorageOptions options, CancellationToken cancellationToken = default)
    {
        return new LocalStorage(bytesPool, options);
    }

    public async ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IDirectory>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (var drive in Directory.GetLogicalDrives())
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(new LocalDirectory(_bytesPool, PathHelper.Normalize(drive), _options.TempDirectoryPath));
            }
        }
        else
        {
            results.Add(new LocalDirectory(_bytesPool, PathHelper.Normalize("/"), _options.TempDirectoryPath));
        }

        return results;
    }
}
