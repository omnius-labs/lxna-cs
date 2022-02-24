using Omnius.Core;

namespace Omnius.Lxna.Components.Storage;

public interface IStorage
{
    IAsyncEnumerable<IDirectory> FindDirectoriesAsync(string tempPath, IBytesPool bytesPool, CancellationToken cancellationToken = default);
}
