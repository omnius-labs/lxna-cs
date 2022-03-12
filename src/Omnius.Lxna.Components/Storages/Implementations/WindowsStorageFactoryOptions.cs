namespace Omnius.Lxna.Components.Storages;

public record WindowsStorageFactoryOptions
{
    public WindowsStorageFactoryOptions(string tempDirectoryPath)
    {
        this.TempDirectoryPath = tempDirectoryPath;
    }

    public string TempDirectoryPath { get; }
}
