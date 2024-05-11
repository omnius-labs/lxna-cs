using Lxna.Components.Image;

namespace Lxna.Components.Thumbnail.Internal.Repositories.Entities;

public class ThumbnailCacheIdEntity
{
    public NestedPathEntity? FilePath { get; set; }
    public ImageResizeType ImageResizeType { get; set; }
    public ImageFormatType ImageFormatType { get; set; }
    public int ThumbnailWidth { get; set; }
    public int ThumbnailHeight { get; set; }
}
