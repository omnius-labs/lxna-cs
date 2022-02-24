using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Thumbnail.Models;

namespace Omnius.Lxna.Components.Thumbnail;

public interface IThumbnailGenerator : IAsyncDisposable
{
    ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(IFile file, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default);
}
