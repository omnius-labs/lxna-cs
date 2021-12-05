using System;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components;

public interface IThumbnailGenerator : IAsyncDisposable
{
    ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(NestedPath filePath, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default);
}
