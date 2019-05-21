using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Lxna.Gui.Desktop.Windows.Main;

namespace Lxna.Gui.Desktop
{
    class Program
    {
        static void Main(string[] args)
        {
            BuildAvaloniaApp().Start<MainWindow>();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .UseDataGrid()
                .LogToDebug();
    }
}
