namespace Omnius.Lxna.Ui.Desktop.Configuration;

public record LxnaEnvironment
{
    public required string StorageDirectoryPath { get; init; }
    public required string DatabaseDirectoryPath { get; init; }
    public required string LogsDirectoryPath { get; init; }
}
