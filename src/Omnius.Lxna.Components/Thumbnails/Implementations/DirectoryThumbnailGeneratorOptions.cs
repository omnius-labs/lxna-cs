namespace Omnius.Lxna.Components.Thumbnails;

public record DirectoryThumbnailGeneratorOptions
{
    public DirectoryThumbnailGeneratorOptions(string configDirectoryPath, int concurrency)
    {
        this.ConfigDirectoryPath = configDirectoryPath;
        this.Concurrency = concurrency;
    }

    public string ConfigDirectoryPath { get; }

    public int Concurrency { get; }
}
