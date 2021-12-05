using System.Threading.Tasks;
using Avalonia.Markup.Xaml;

namespace Omnius.Lxna.Ui.Desktop.Windows;

public partial class MainWindow : StatefulWindowBase
{
    public MainWindow()
        : base()
    {
        this.InitializeComponent();
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
