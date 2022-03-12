namespace Omnius.Lxna.Components.ThumbnailGenerators;

public record WindowsThumbnailGeneratorFatcoryOptions
{
    public WindowsThumbnailGeneratorFatcoryOptions(string configDirectoryPath, int concurrency)
    {
        this.ConfigDirectoryPath = configDirectoryPath;
        this.Concurrency = concurrency;
    }

    public string ConfigDirectoryPath { get; }

    public int Concurrency { get; }
}
