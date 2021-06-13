using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Windows.Primitives;

namespace Omnius.Lxna.Ui.Desktop.Windows.PicturePreview
{
    public class PicturePreviewWindow : StatefulWindowBase
    {
        public PicturePreviewWindow()
            : base()
        {
            this.InitializeComponent();

            this.ViewModel = Bootstrapper.ServiceProvider!.GetRequiredService<PicturePreviewWindowViewModel>();
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

        public PicturePreviewWindowViewModel ViewModel { get; }
    }
}
