namespace Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common.Repositories.Entities;

public class ThumbnailCacheIdEntity
{
    public NestedPathEntity? FilePath { get; set; }
    public ThumbnailResizeType ThumbnailResizeType { get; set; }
    public ThumbnailFormatType ThumbnailFormatType { get; set; }
    public int ThumbnailWidth { get; set; }
    public int ThumbnailHeight { get; set; }
}
