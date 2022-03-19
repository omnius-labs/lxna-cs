using Omnius.Core;
using Omnius.Lxna.Components.Storages.Internal.Windows.Helpers;

namespace Omnius.Lxna.Components.Storages;

internal sealed class WindowsStorage : IStorage
{
    private readonly string _tempPath;
    private readonly IBytesPool _bytesPool;

    public WindowsStorage(IBytesPool bytesPool, string tempPath)
    {
        _tempPath = tempPath;
        _bytesPool = bytesPool;
    }

    public async ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IDirectory>();

        foreach (var drive in Directory.GetLogicalDrives())
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(new Internal.Windows.LocalDirectory(_bytesPool, PathHelper.Normalize(drive), _tempPath));
        }

        return results;
    }
}
