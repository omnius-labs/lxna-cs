using System.Threading.Tasks;
using Avalonia.Markup.Xaml;

namespace Omnius.Lxna.Ui.Desktop.Windows;

public class PicturePreviewWindow : StatefulWindowBase
{
    public PicturePreviewWindow()
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
        if (this.ViewModel is PicturePreviewWindowViewModel viewModel)
        {
            await viewModel.DisposeAsync();
        }
    }

    public PicturePreviewWindowViewModel? ViewModel
    {
        get => this.DataContext as PicturePreviewWindowViewModel;
        set => this.DataContext = value;
    }

}
