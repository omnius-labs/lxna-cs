namespace Omnius.Lxna.Components.Thumbnail.Models;

public record ThumbnailGeneratorOptions
{
    public ThumbnailGeneratorOptions(string configDirectoryPath, int concurrency)
    {
        this.ConfigDirectoryPath = configDirectoryPath;
        this.Concurrency = concurrency;
    }

    public string ConfigDirectoryPath { get; }

    public int Concurrency { get; }
}
