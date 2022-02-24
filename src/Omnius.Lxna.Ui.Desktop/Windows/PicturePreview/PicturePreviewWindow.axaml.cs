using System.ComponentModel;
using Avalonia;
using Avalonia.Markup.Xaml;
using Omnius.Core.Avalonia;
using Omnius.Lxna.Ui.Desktop.Configuration;

namespace Omnius.Axis.Ui.Desktop.Windows.PicturePreview;

public partial class PicturePreviewWindow : StatefulWindowBase<PicturePreviewWindowViewModelBase>
{
    private string? _result = null;

    public PicturePreviewWindow()
        : base()
    {
        this.InitializeComponent();
        this.GetObservable(ViewModelProperty).Subscribe(this.OnViewModelChanged);
        this.Closing += new EventHandler<CancelEventArgs>((_, _) => this.OnClosing());
        this.Closed += new EventHandler((_, _) => this.OnClosed());
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public string? GetResult() => _result;

    private void OnViewModelChanged(PicturePreviewWindowViewModelBase? viewModel)
    {
        if (viewModel?.Status is PicturePreviewWindowStatus status)
        {
            this.SetWindowStatus(status.Window);
        }
    }

    private void OnClosing()
    {
        if (this.ViewModel?.Status is PicturePreviewWindowStatus status)
        {
            status.Window = this.GetWindowStatus();
        }
    }

    private async void OnClosed()
    {
        if (this.ViewModel is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}
