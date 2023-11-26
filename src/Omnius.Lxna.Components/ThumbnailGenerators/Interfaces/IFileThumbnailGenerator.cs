using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.ThumbnailGenerators.Models;

namespace Omnius.Lxna.Components.ThumbnailGenerators;

public interface IFileThumbnailGenerator : IAsyncDisposable
{
    ValueTask<FileThumbnailResult> GenerateAsync(IFile file, FileThumbnailOptions options, bool isCacheOnly, CancellationToken cancellationToken = default);
}
