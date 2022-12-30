namespace Omnius.Lxna.Components.Thumbnails.Models;

public record FileThumbnailOptions
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required ThumbnailFormatType FormatType { get; init; }
    public required ThumbnailResizeType ResizeType { get; init; }
    public required TimeSpan MinInterval { get; init; }
    public required int MaxImageCount { get; init; }
}
