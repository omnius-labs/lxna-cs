namespace Omnius.Lxna.Components.Thumbnails;

public record DirectoryThumbnailGeneratorOptions
{
    public required string ConfigDirectoryPath { get; init; }
    public required int Concurrency { get; init; }
}
