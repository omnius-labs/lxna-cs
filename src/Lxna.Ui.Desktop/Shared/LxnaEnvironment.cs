namespace Lxna.Ui.Desktop.Shared;

public record LxnaEnvironment
{
    public required string StorageDirectoryPath { get; init; }
    public required string StateDirectoryPath { get; init; }
    public required string LogsDirectoryPath { get; init; }
}
