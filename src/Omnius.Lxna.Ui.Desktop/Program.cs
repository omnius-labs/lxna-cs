using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Omnius.Lxna.Ui.Desktop.Windows.Main;

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
