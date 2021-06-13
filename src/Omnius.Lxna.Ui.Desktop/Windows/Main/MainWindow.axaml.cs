using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Omnius.Lxna.Ui.Desktop.Windows.Primitives;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main
{
    public partial class MainWindow : StatefulWindowBase
    {
        public MainWindow()
            : base()
        {
            this.InitializeComponent();

            this.ViewModel = Bootstrapper.ServiceProvider!.GetRequiredService<MainWindowViewModel>();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override async ValueTask OnInitializeAsync()
        {
        }

        protected override async ValueTask OnDisposeAsync()
        {
            if (this.ViewModel is MainWindowViewModel mainWindowViewModel)
            {
                await mainWindowViewModel.DisposeAsync();
            }
        }

        public MainWindowViewModel? ViewModel
        {
            get => this.DataContext as MainWindowViewModel;
            set => this.DataContext = value;
        }
    }
}
