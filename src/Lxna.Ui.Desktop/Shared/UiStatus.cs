using Core.Avalonia;
using Core.Base.Helpers;
using Core.Utils;

namespace Lxna.Ui.Desktop.Shared;

public sealed class UiStatus
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public int Version { get; init; }
    public MainWindowStatus? MainWindow { get; set; }
    public ExplorerViewStatus? ExplorerView { get; set; }
    public PreviewWindowStatus? PicturePreview { get; set; }
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

        result ??= new UiStatus()
        {
            ExplorerView = new ExplorerViewStatus()
            {
                TreeViewWidth = 240,
            }
        };

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

public sealed class PreviewWindowStatus : BindableBase
{
}

public sealed class SettingsWindowStatus
{
}
