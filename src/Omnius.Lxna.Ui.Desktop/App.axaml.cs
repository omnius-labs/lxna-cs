using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Omnius.Lxna.Ui.Desktop.Windows.Views.Main;

namespace Omnius.Lxna.Ui.Desktop
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static new App Current => (App)Application.Current;

        public Window? MainWindow => (this.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        public override void OnFrameworkInitializationCompleted()
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
