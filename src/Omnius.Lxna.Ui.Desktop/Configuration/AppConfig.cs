using Omnius.Core.Utils;

namespace Omnius.Lxna.Ui.Desktop.Configuration;

public class AppConfig
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public int Version { get; init; }

    public bool Verbose { get; init; }

    public string? StorageDirectoryPath { get; init; }

    public string? LogsDirectoryPath { get; init; }

    public static async ValueTask<AppConfig> LoadAsync(string configPath)
    {
        AppConfig? result = null;

        try
        {
            result = YamlHelper.ReadFile<AppConfig>(configPath);
        }
        catch (Exception e)
        {
            _logger.Debug(e);
        }

        result ??= new AppConfig()
        {
            Version = 1,
            StorageDirectoryPath = "storage",
            LogsDirectoryPath = "logs",
            Verbose = false,
        };

        return result;
    }

    public async ValueTask SaveAsync(string configPath)
    {
        YamlHelper.WriteFile(configPath, this);
    }
}
