using Avalonia;
using Avalonia.Logging.Serilog;

namespace Omnius.Lxna.Ui.Desktop
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = BuildAvaloniaApp();
            builder.StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .LogToDebug();
    }
}
