using Omnius.Core.Avalonia.Models;
using Omnius.Core.Helpers;
using Omnius.Core.Utils;

namespace Omnius.Lxna.Ui.Desktop.Configuration;

public sealed partial class UiStatus
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public int Version { get; init; }

    public MainWindowStatus? MainWindow { get; set; }

    public PicturePreviewWindowStatus? PicturePreview { get; set; }

    public static async ValueTask<UiStatus> LoadAsync(string configPath)
    {
        UiStatus? result = null;

        try
        {
            result = await JsonHelper.ReadFileAsync<UiStatus>(configPath);
        }
        catch (Exception e)
        {
            _logger.Debug(e);
        }

        result ??= new UiStatus();

        return result;
    }

    public async ValueTask SaveAsync(string configPath)
    {
        DirectoryHelper.CreateDirectory(Path.GetDirectoryName(configPath)!);
        await JsonHelper.WriteFileAsync(configPath, this, true);
    }
}

public sealed class MainWindowStatus
{
    public WindowStatus? Window { get; set; }
}


public sealed class PicturePreviewWindowStatus
{
    public WindowStatus? Window { get; set; }
}
