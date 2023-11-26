using Omnius.Core.Collections;

namespace Omnius.Lxna.Components.ThumbnailGenerators.Models;

public readonly struct FileThumbnailResult
{
    public FileThumbnailResult(FileThumbnailResultStatus status, IEnumerable<ThumbnailContent>? contents = null)
    {
        this.Status = status;
        this.Contents = new ReadOnlyListSlim<ThumbnailContent>(contents?.ToArray() ?? Array.Empty<ThumbnailContent>());
    }

    public FileThumbnailResultStatus Status { get; }
    public ReadOnlyListSlim<ThumbnailContent> Contents { get; }
}

public enum FileThumbnailResultStatus
{
    Unknown,
    Succeeded,
    Failed,
}
