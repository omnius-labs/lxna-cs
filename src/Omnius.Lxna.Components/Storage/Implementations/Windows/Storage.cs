using System.Runtime.CompilerServices;
using Omnius.Core;
using Omnius.Lxna.Components.Storage.Windows.Internal.Helpers;

namespace Omnius.Lxna.Components.Storage.Windows;

public sealed class Storage : IStorage
{
    public async IAsyncEnumerable<IDirectory> FindDirectoriesAsync(string tempPath, IBytesPool bytesPool, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var drive in Directory.GetLogicalDrives())
        {
            yield return new LocalDirectory(PathHelper.Normalize(drive), tempPath, bytesPool);
        }
    }
}
