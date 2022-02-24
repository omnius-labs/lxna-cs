using Omnius.Core.Collections;

namespace Omnius.Lxna.Components.Thumbnail.Models;

public readonly struct ThumbnailGeneratorGetThumbnailResult
{
    public ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus status, IEnumerable<ThumbnailContent>? contents = null)
    {
        this.Status = status;
        this.Contents = new ReadOnlyListSlim<ThumbnailContent>(contents?.ToArray() ?? Array.Empty<ThumbnailContent>());
    }

    public ThumbnailGeneratorGetThumbnailResultStatus Status { get; }

    public ReadOnlyListSlim<ThumbnailContent> Contents { get; }
}

public enum ThumbnailGeneratorGetThumbnailResultStatus
{
    Unknown,
    Succeeded,
    Failed,
}
