namespace Omnius.Lxna.Components.Storages;

public record LocalStorageOptions
{
    public required string TempDirectoryPath { get; init; }
    public required string RootDirectoryPath { get; init; }
}
