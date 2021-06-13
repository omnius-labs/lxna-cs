using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Omnius.Lxna.Ui.Desktop.Windows.Main;

namespace Omnius.Lxna.Ui.Desktop
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static new App Current => (App)Application.Current;

        public IClassicDesktopStyleApplicationLifetime? Lifetime => (this.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);

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
