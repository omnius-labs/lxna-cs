using System.Runtime.CompilerServices;
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

    public async IAsyncEnumerable<IDirectory> FindDirectoriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var drive in Directory.GetLogicalDrives())
        {
            yield return new Internal.Windows.LocalDirectory(_bytesPool, PathHelper.Normalize(drive), _tempPath);
        }
    }
}
