using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.ThumbnailGenerators.Models;

namespace Omnius.Lxna.Components.ThumbnailGenerators;

public interface IThumbnailGenerator : IAsyncDisposable
{
    ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(IFile file, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default);

    ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(IDirectory directory, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default);
}
