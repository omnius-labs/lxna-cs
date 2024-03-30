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

        _panel = this.FindControl<Panel>("Panel") ?? throw new NullReferenceException();

        {
            var sizeChangedObservable = Observable.FromEventPattern<SizeChangedEventArgs>(
                h => _panel.SizeChanged += h,
                h => _panel.SizeChanged -= h)
                .Select(e => e.EventArgs.NewSize);
            var firstEventObservable = sizeChangedObservable
                .Take(1);
            var remainingEventsObservable = sizeChangedObservable
                .Skip(1)
                .Throttle(TimeSpan.FromSeconds(2));
            var mergedObservable = firstEventObservable
                .Concat(remainingEventsObservable);
            mergedObservable
                .Subscribe(this.PanelOnSizeChanged)
                .AddTo(_disposable);
        }

        {
            var wheelScrollObservable = Observable.FromEventPattern<PointerWheelEventArgs>(
                h => _panel.PointerWheelChanged += h,
                h => _panel.PointerWheelChanged -= h)
                .Select(e => e.EventArgs.Delta.Y);
            var sampledObservable = wheelScrollObservable
                .Sample(TimeSpan.FromMilliseconds(500));
            sampledObservable
                .Subscribe(this.PanelOnPointerWheelChanged)
                .AddTo(_disposable);
        }
    }

    private async void OnClosed()
    {
        if (this.DataContext is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }

        _disposable.Dispose();
    }

    private void PanelOnPointerWheelChanged(double y)
    {
        Dispatcher.UIThread.Invoke(() =>
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
        });
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
