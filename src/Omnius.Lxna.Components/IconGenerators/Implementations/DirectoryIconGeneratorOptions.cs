namespace Omnius.Lxna.Components.IconGenerators;

public record DirectoryIconGeneratorOptions
{
    public required string ConfigDirectoryPath { get; init; }
    public required int Concurrency { get; init; }
}
