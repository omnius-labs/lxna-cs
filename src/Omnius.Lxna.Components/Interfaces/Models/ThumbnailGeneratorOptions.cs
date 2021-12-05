using Omnius.Core;

namespace Omnius.Lxna.Components.Models;

public record ThumbnailGeneratorOptions
{
    public ThumbnailGeneratorOptions(string configDirectoryPath, int concurrency, IFileSystem fileSystem, IBytesPool bytesPool)
    {
        this.ConfigDirectoryPath = configDirectoryPath;
        this.Concurrency = concurrency;
        this.FileSystem = fileSystem;
        this.BytesPool = bytesPool;
    }

    public string ConfigDirectoryPath { get; }
    public int Concurrency { get; }
    public IFileSystem FileSystem { get; }
    public IBytesPool BytesPool { get; }
}
