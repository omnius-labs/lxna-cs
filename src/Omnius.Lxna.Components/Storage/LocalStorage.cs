using System.Runtime.InteropServices;
using Omnius.Core;
using Omnius.Lxna.Components.Storage.Internal;
using Omnius.Lxna.Components.Storage.Internal.Helpers;

namespace Omnius.Lxna.Components.Storage;

public record LocalStorageOptions
{
    public required string TempDirectoryPath { get; init; }
    public string? RootDirectoryPath { get; set; }
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
        if (_options.RootDirectoryPath is string rootDirectoryPath)
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
                return [new LocalDirectory(_bytesPool, PathHelper.Normalize("/"), _options.TempDirectoryPath)];
            }
        }
    }
}
