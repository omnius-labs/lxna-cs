using Omnius.Core.Avalonia;
using Omnius.Core.Helpers;
using Omnius.Core.Utils;

namespace Omnius.Lxna.Ui.Desktop.Configuration;

public sealed class UiStatus
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public int Version { get; init; }
    public MainWindowStatus? MainWindow { get; set; }
    public ExplorerViewStatus? ExplorerView { get; set; }
    public PicturePreviewWindowStatus? PicturePreview { get; set; }
    public SettingsWindowStatus? SettingsWindow { get; set; }

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

public sealed class MainWindowStatus : BindableBase
{
}

public sealed class ExplorerViewStatus : BindableBase
{
    private double _treeViewWidth;

    public double TreeViewWidth
    {
        get => _treeViewWidth;
        set => this.SetProperty(ref _treeViewWidth, value);
    }
}

public sealed class PicturePreviewWindowStatus : BindableBase
{
}

public sealed class SettingsWindowStatus
{
}
