namespace Omnius.Lxna.Components.Storages;

public record LxnaStorageOptions
{
    public required string TempDirectoryPath { get; init; }
    public string? RootDirectoryPath { get; init; }
}
