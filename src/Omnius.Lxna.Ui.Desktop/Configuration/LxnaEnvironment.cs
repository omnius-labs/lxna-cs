namespace Omnius.Lxna.Ui.Desktop.Configuration;

public sealed class LxnaEnvironment
{
    public LxnaEnvironment(string storageDirectoryPath, string databaseDirectoryPath, string logsDirectoryPath)
    {
        this.StorageDirectoryPath = storageDirectoryPath;
        this.DatabaseDirectoryPath = databaseDirectoryPath;
        this.LogsDirectoryPath = logsDirectoryPath;
    }

    public string StorageDirectoryPath { get; }
    public string DatabaseDirectoryPath { get; }
    public string LogsDirectoryPath { get; }
}
