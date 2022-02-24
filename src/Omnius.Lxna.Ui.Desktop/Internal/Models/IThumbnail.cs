using Avalonia.Media.Imaging;
using Omnius.Lxna.Components.Thumbnail.Models;

namespace Omnius.Lxna.Ui.Desktop.Internal.Models;

public interface IThumbnail : IDisposable
{
    string Name { get; }

    Bitmap? Thumbnail { get; }

    bool IsRotatableThumbnail { get; }

    ValueTask SetThumbnailAsync(IEnumerable<ThumbnailContent> thumbnailContents, CancellationToken cancellationToken = default);

    ValueTask ClearThumbnailAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> TryRotateThumbnailAsync(CancellationToken cancellationToken = default);
}
