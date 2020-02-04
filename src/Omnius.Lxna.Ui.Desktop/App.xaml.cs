using Avalonia;
using Avalonia.Markup.Xaml;

namespace Lxna.Gui.Desktop
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
