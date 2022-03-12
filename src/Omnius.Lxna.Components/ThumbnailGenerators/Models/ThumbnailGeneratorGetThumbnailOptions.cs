namespace Omnius.Lxna.Components.ThumbnailGenerators.Models;

public readonly struct ThumbnailGeneratorGetThumbnailOptions
{
    public ThumbnailGeneratorGetThumbnailOptions(int width, int height, ThumbnailFormatType formatType, ThumbnailResizeType resizeType, TimeSpan minInterval, int maxImageCount)
    {
        this.Width = width;
        this.Height = height;
        this.FormatType = formatType;
        this.ResizeType = resizeType;
        this.MinInterval = minInterval;
        this.MaxImageCount = maxImageCount;
    }

    public int Width { get; }

    public int Height { get; }

    public ThumbnailFormatType FormatType { get; }

    public ThumbnailResizeType ResizeType { get; }

    public TimeSpan MinInterval { get; }

    public int MaxImageCount { get; }
}
