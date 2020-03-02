using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Omnius.Core;
using Omnius.Lxna.Service;
using Omnius.Lxna.Ui.Desktop.Views.Main;

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
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                var configPath = Path.Combine(Directory.GetCurrentDirectory(), "../config");
                Directory.CreateDirectory(configPath);

                var thumbnailGenerator = ThumbnailGenerator.Factory.CreateAsync(configPath, ObjectStore.Factory, BytesPool.Shared).Result;
                var mainViewModel = new MainViewModel(thumbnailGenerator);

                desktopLifetime.MainWindow = new MainView(mainViewModel);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
