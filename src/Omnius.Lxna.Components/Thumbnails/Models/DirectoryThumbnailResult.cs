using Omnius.Core.Collections;

namespace Omnius.Lxna.Components.Thumbnails.Models;

public readonly struct DirectoryThumbnailResult
{
    public DirectoryThumbnailResult(DirectoryThumbnailResultStatus status, IEnumerable<ThumbnailContent>? contents = null)
    {
        this.Status = status;
        this.Contents = new ReadOnlyListSlim<ThumbnailContent>(contents?.ToArray() ?? Array.Empty<ThumbnailContent>());
    }

    public DirectoryThumbnailResultStatus Status { get; }

    public ReadOnlyListSlim<ThumbnailContent> Contents { get; }
}

public enum DirectoryThumbnailResultStatus
{
    Unknown,
    Succeeded,
    Failed,
}
