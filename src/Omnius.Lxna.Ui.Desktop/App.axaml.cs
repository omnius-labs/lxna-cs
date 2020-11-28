using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Omnius.Core;
using Omnius.Lxna.Components;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.ViewModels;
using Omnius.Lxna.Ui.Desktop.Views;

namespace Omnius.Lxna.Ui.Desktop
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var configPath = Path.Combine(Directory.GetCurrentDirectory(), "../config");
                Directory.CreateDirectory(configPath);

                var thumbnailGenerator = ThumbnailGenerator.Factory.CreateAsync(configPath, new ThumbnailGeneratorOptions(8), BytesPool.Shared).Result;
                var mainWindowViewModel = new MainWindowViewModel(thumbnailGenerator);

                desktop.MainWindow = new MainWindow() { ViewModel = mainWindowViewModel };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
