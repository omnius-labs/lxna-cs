namespace Omnius.Lxna.Components.Thumbnails;

public record FileThumbnailGeneratorOptions
{
    public required string ConfigDirectoryPath { get; init; }
    public required int Concurrency { get; init; }
}
