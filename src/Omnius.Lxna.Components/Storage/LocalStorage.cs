using System.Runtime.InteropServices;
using Omnius.Core;
using Omnius.Lxna.Components.Storage.Internal;
using Omnius.Lxna.Components.Storage.Internal.Helpers;

namespace Omnius.Lxna.Components.Storage;

public record LocalStorageOptions
{
    public required string TempDirectoryPath { get; init; }
}

public sealed class LocalStorage : IStorage
{
    private readonly LocalStorageOptions _options;
    private readonly IBytesPool _bytesPool;

    public LocalStorage(IBytesPool bytesPool, LocalStorageOptions options)
    {
        _options = options;
        _bytesPool = bytesPool;
    }

    public async ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(string? rootDirectoryPath = null, CancellationToken cancellationToken = default)
    {
        if (rootDirectoryPath is not null)
        {
            return [new LocalDirectory(_bytesPool, PathHelper.Normalize(rootDirectoryPath), _options.TempDirectoryPath)];
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var results = new List<IDirectory>();

                foreach (var drive in Directory.GetLogicalDrives())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    results.Add(new LocalDirectory(_bytesPool, PathHelper.Normalize(drive), _options.TempDirectoryPath));
                }

                return results;
            }
            else
            {
                var results = new List<IDirectory>();

                foreach (var drive in Directory.GetDirectories("/"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    results.Add(new LocalDirectory(_bytesPool, PathHelper.Normalize(drive), _options.TempDirectoryPath));
                }

                return results;
            }
        }
    }
}
