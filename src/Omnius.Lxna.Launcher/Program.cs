using System.Globalization;
using System.Reflection;

namespace Omnius.Lxna.Launcher;

public class Program
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public static async Task Main(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        SetLogsDirectory(Path.Combine(basePath, "storage/launcher/logs"));

        _logger.Info("---- Start ----");
        _logger.Info(CultureInfo.InvariantCulture, "AssemblyInformationalVersion: {0}", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

        try
        {
            // zipがダウンロード済みの場合、アップデートを実行する
            if (Updater.TryUpdate()) return;

            // バックグラウンドで、新しいバージョンをチェックし、新しいバージョンが見つかればダウンロードする
            Downloader.Download();

            // 本体を起動する
            await Runner.RunAsync();
        }
        catch (Exception e)
        {
            _logger.Error(e, $"Failed");
        }
        finally
        {
            _logger.Info("---- End ----");
            NLog.LogManager.Shutdown();
        }
    }

    private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.Error(e.ExceptionObject as Exception, "Unexpected Exception");
    }

    private static void SetLogsDirectory(string logsDirectoryPath)
    {
        var target = (NLog.Targets.FileTarget)NLog.LogManager.Configuration.FindTargetByName("log_file");
        target.FileName = $"{Path.GetFullPath(logsDirectoryPath)}/${{date:format=yyyy-MM-dd}}.log";
        target.ArchiveFileName = $"{Path.GetFullPath(logsDirectoryPath)}/logs/archive.{{#}}.log";
        NLog.LogManager.ReconfigExistingLoggers();
    }
}
