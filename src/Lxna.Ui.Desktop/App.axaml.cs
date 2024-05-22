using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommandLine;
using Core.Base.Helpers;
using Lxna.Ui.Desktop.Shared;
using Lxna.Ui.Desktop.View.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Lxna.Ui.Desktop;

public class App : Application
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private FileStream? _lockFileStream;
    private Updater? _updater;

    public override void Initialize()
    {
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((_, e) => _logger.Error(e));

        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Exit += (_, _) => this.Exit();
        }

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (!Design.IsDesignMode)
        {
            this.Startup();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static new App Current => (App)Application.Current!;
    public new IClassicDesktopStyleApplicationLifetime ApplicationLifetime => (IClassicDesktopStyleApplicationLifetime)base.ApplicationLifetime!;
    public MainWindow MainWindow => (MainWindow)this.ApplicationLifetime?.MainWindow!;

    private void Startup()
    {
        this.Init();

        var parsedResult = CommandLine.Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs());
        parsedResult.WithParsed(this.OnParsed);
    }

    private void Init()
    {
        var configFiles = ImageMagick.Configuration.ConfigurationFiles.Default;
        configFiles.Policy.Data = @"
<policymap>
  <policy domain=""delegate"" rights=""none"" pattern=""*"" />
  <policy domain=""filter"" rights=""none"" pattern=""*"" />
  <policy domain=""coder"" rights=""none"" pattern=""*"" />
  <policy domain=""coder"" rights=""read|write"" pattern=""{GIF,JPEG,PNG,WEBP,BMP,HEIF,HEIC,AVIF,SVG}"" />
</policymap>";
        ImageMagick.MagickNET.Initialize(configFiles);
    }

    public class Options
    {
        [Option('s', "storage")]
        public string StorageDirectoryPath { get; set; } = "../storage/ui-desktop";

        [Option('v', "verbose")]
        public bool Verbose { get; set; } = false;
    }

    private async void OnParsed(Options options)
    {
        try
        {
            DirectoryHelper.CreateDirectory(options.StorageDirectoryPath);

            var lxnaEnvironment = new LxnaEnvironment()
            {
                StorageDirectoryPath = options.StorageDirectoryPath,
                StateDirectoryPath = Path.Combine(options.StorageDirectoryPath, "state"),
                LogsDirectoryPath = Path.Combine(options.StorageDirectoryPath, "logs"),
            };

            DirectoryHelper.CreateDirectory(lxnaEnvironment.StateDirectoryPath);
            DirectoryHelper.CreateDirectory(lxnaEnvironment.LogsDirectoryPath);

            SetLogsDirectory(lxnaEnvironment.LogsDirectoryPath);

            if (options.Verbose) ChangeLogLevel(NLog.LogLevel.Trace);

            _lockFileStream = new FileStream(Path.Combine(options.StorageDirectoryPath, "lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);

            _logger.Info("---- Start ----");
            _logger.Info($"AssemblyInformationalVersion: {Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");

            _updater = await Updater.CreateAsync();

            if (_updater.TryExecute())
            {
                _logger.Info("Update start");
                this.ApplicationLifetime.Shutdown();
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                _logger.Info($"AVALONIA_SCREEN_SCALE_FACTORS: {Environment.GetEnvironmentVariable("AVALONIA_SCREEN_SCALE_FACTORS")}");
            }

            var mainWindow = new MainWindow(Path.Combine(lxnaEnvironment.StateDirectoryPath, "windows", "main"));

            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.MainWindow = mainWindow;
            }

            await Bootstrapper.Instance.BuildAsync(lxnaEnvironment);

            var serviceProvider = Bootstrapper.Instance.GetServiceProvider();
            mainWindow.DataContext = serviceProvider.GetRequiredService<MainWindowModel>();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unexpected Exception");
            throw;
        }
    }

    private void SetLogsDirectory(string logsDirectoryPath)
    {
        var target = (NLog.Targets.FileTarget)NLog.LogManager.Configuration.FindTargetByName("log_file");
        target.FileName = $"{Path.GetFullPath(logsDirectoryPath)}/${{date:format=yyyy-MM-dd}}.log";
        target.ArchiveFileName = $"{Path.GetFullPath(logsDirectoryPath)}/archives/{{#}}.log";
        NLog.LogManager.ReconfigExistingLoggers();
    }

    private void ChangeLogLevel(NLog.LogLevel minLevel)
    {
        _logger.Debug("Log level changed: {0}", minLevel);

        var rootLoggingRule = NLog.LogManager.Configuration.LoggingRules.First(n => n.NameMatches("*"));
        rootLoggingRule.EnableLoggingForLevels(minLevel, NLog.LogLevel.Fatal);
        NLog.LogManager.ReconfigExistingLoggers();
    }

    private async void Exit()
    {
        await Bootstrapper.Instance.DisposeAsync();

        if (_updater is not null) await _updater.DisposeAsync();

        _logger.Info("---- End ----");
        NLog.LogManager.Shutdown();

        _lockFileStream?.Dispose();
    }
}
