using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components;

public interface IFileSystem : IAsyncDisposable
{
    ValueTask<bool> ExistsFileAsync(NestedPath path, CancellationToken cancellationToken = default);

    ValueTask<bool> ExistsDirectoryAsync(NestedPath path, CancellationToken cancellationToken = default);

    ValueTask<DateTime> GetFileLastWriteTimeAsync(NestedPath path, CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<NestedPath>> FindDirectoriesAsync(NestedPath? path = null, CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<NestedPath>> FindArchiveFilesAsync(NestedPath path, CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<NestedPath>> FindFilesAsync(NestedPath path, CancellationToken cancellationToken = default);

    ValueTask<Stream> GetFileStreamAsync(NestedPath path, CancellationToken cancellationToken = default);

    ValueTask<long> GetFileSizeAsync(NestedPath path, CancellationToken cancellationToken = default);

    ValueTask<IExtractedFileOwner?> TryExtractFileAsync(NestedPath path, CancellationToken cancellationToken = default);
}
