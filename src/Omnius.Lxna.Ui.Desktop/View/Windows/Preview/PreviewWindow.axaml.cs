using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Omnius.Core.Avalonia;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public partial class PreviewWindow : RestorableWindow
{
    private Panel _panel = null!;
    private Image _image = null!;

    public PreviewWindow()
        : base()
    {
        this.InitializeComponent();
    }

    public PreviewWindow(string configDirectoryPath)
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

        _panel = this.FindControl<Panel>("Panel") ?? throw new NullReferenceException();
        _image = this.FindControl<Image>("Image") ?? throw new NullReferenceException();

        _panel.SizeChanged += this.OnPanelSizeChanged;
    }

    private async void OnClosed()
    {
        if (this.DataContext is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (this.DataContext is PreviewWindowModel viewModel)
        {
            if (e.Delta.Y < 0)
            {
                viewModel.NotifyNext();
            }
            else if (e.Delta.Y > 0)
            {
                viewModel.NotifyPrev();
            }
        }
    }

    private void OnPanelSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (this.DataContext is PreviewWindowModel viewModel)
        {
            viewModel.NotifyImageSizeChanged(e.NewSize);
        }
    }
}
