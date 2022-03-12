using Avalonia.Media.Imaging;
using Omnius.Lxna.Components.ThumbnailGenerators.Models;

namespace Omnius.Lxna.Ui.Desktop.Internal.Models;

public interface IThumbnail<out T> : IDisposable
{
    T Target { get; }

    string Name { get; }

    Bitmap? Image { get; }

    bool IsRotatable { get; }

    void Set(IEnumerable<ThumbnailContent> thumbnailContents);

    void Clear();

    bool TryRotate();
}
