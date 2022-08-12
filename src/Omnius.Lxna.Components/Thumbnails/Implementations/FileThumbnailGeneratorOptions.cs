namespace Omnius.Lxna.Components.Thumbnails;

public record FileThumbnailGeneratorOptions
{
    public FileThumbnailGeneratorOptions(string configDirectoryPath, int concurrency)
    {
        this.ConfigDirectoryPath = configDirectoryPath;
        this.Concurrency = concurrency;
    }

    public string ConfigDirectoryPath { get; }

    public int Concurrency { get; }
}
