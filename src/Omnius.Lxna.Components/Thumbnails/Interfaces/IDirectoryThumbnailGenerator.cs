using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.Thumbnails.Models;

namespace Omnius.Lxna.Components.Thumbnails;

public interface IDirectoryThumbnailGenerator : IAsyncDisposable
{
    ValueTask<DirectoryThumbnailResult> GenerateAsync(IDirectory directory, DirectoryThumbnailOptions options, bool isCacheOnly, CancellationToken cancellationToken = default);
}
