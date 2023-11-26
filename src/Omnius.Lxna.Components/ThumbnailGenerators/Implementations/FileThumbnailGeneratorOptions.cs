namespace Omnius.Lxna.Components.ThumbnailGenerators;

public record FileThumbnailGeneratorOptions
{
    public required string ConfigDirectoryPath { get; init; }
    public required int Concurrency { get; init; }
}
