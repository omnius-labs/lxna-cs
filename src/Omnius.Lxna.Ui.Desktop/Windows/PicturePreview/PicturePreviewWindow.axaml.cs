using Avalonia;
using Avalonia.Markup.Xaml;
using Omnius.Core.Avalonia;

namespace Omnius.Axis.Ui.Desktop.Windows.PicturePreview;

public partial class PicturePreviewWindow : StatefulWindowBase<PicturePreviewWindowModelBase>
{
    public PicturePreviewWindow()
        : base()
    {
        this.InitializeComponent();
    }

    public PicturePreviewWindow(string configDirectoryPath)
        : base(configDirectoryPath)
    {
        this.InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        this.Closed += new EventHandler((_, _) => this.OnClosed());
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnClosed()
    {
        if (this.ViewModel is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}
