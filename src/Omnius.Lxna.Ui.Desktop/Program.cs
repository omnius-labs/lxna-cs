using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Omnius.Core.Helpers;

namespace Omnius.Lxna.Ui.Desktop
{
    public class Program
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public static async Task Main(string[] args)
        {
            string stateDirectoryPath = args[0];
            string temporaryDirectoryPath = args[1];
            string logsDirectoryPath = args[2];

            DirectoryHelper.CreateDirectory(stateDirectoryPath);
            DirectoryHelper.CreateDirectory(temporaryDirectoryPath);
            DirectoryHelper.CreateDirectory(logsDirectoryPath);

            SetLogsDirectory(logsDirectoryPath);
#if DEBUG
            ChangeLogLevel(NLog.LogLevel.Trace);
#endif

            _logger.Info("desktop-ui start");

            await Bootstrapper.RegisterAsync(stateDirectoryPath, temporaryDirectoryPath);

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
        }

        private static void SetLogsDirectory(string logsDirectoryPath)
        {
            var target = (NLog.Targets.FileTarget)NLog.LogManager.Configuration.FindTargetByName("log_file");
            target.FileName = $"{Path.GetFullPath(logsDirectoryPath)}/${{date:format=yyyy-MM-dd}}.log";
            target.ArchiveFileName = $"{Path.GetFullPath(logsDirectoryPath)}/logs/archive.{{#}}.log";
            NLog.LogManager.ReconfigExistingLoggers();
        }

        private static void ChangeLogLevel(NLog.LogLevel logLevel)
        {
            var rootLoggingRule = NLog.LogManager.Configuration.LoggingRules.First(n => n.NameMatches("*"));
            rootLoggingRule.EnableLoggingForLevel(logLevel);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
