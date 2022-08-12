using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.Thumbnails.Models;

namespace Omnius.Lxna.Components.Thumbnails;

public interface IFileThumbnailGenerator : IAsyncDisposable
{
    ValueTask<FileThumbnailResult> GenerateAsync(IFile file, FileThumbnailOptions options, bool isCacheOnly, CancellationToken cancellationToken = default);
}
