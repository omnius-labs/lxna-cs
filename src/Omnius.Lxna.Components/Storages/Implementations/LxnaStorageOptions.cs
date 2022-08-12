namespace Omnius.Lxna.Components.Storages;

public record LxnaStorageOptions
{
    public LxnaStorageOptions(string tempDirectoryPath)
    {
        this.TempDirectoryPath = tempDirectoryPath;
    }

    public string TempDirectoryPath { get; }
}
