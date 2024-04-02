using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Omnius.Core.Avalonia;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public partial class PreviewWindow : RestorableWindow
{
    private Panel _panel = null!;

    private readonly CompositeDisposable _disposable = new();

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

        this.PointerWheelChanged += (sender, e) => this.OnPointerWheelChanged(e.Delta.Y);

        _panel = this.FindControl<Panel>("Panel") ?? throw new NullReferenceException();
        _panel.SizeChanged += (sender, e) => this.PanelOnSizeChanged(e.NewSize);
    }

    private async void OnClosed()
    {
        if (this.DataContext is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }

        _disposable.Dispose();
    }

    private void OnPointerWheelChanged(double y)
    {
        if (this.DataContext is PreviewWindowModel viewModel)
        {
            if (y < 0)
            {
                viewModel.NotifyNext();
            }
            else if (y > 0)
            {
                viewModel.NotifyPrev();
            }
        }
    }

    private void PanelOnSizeChanged(Size newSize)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (this.DataContext is PreviewWindowModel viewModel)
            {
                viewModel.NotifyImageSizeChanged(newSize);
            }
        });
    }
}
