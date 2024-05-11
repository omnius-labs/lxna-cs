using System.Collections.Immutable;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Core.Avalonia;
using Core.Base;
using Core.Streams;
using Lxna.Components.Storage;
using Lxna.Ui.Desktop.Service.Internal;
using Lxna.Ui.Desktop.Service.Preview;
using Lxna.Ui.Desktop.Shared;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Lxna.Ui.Desktop.View.Windows;

public abstract class PreviewWindowModelBase : AsyncDisposableBase
{
    public PreviewWindowStatus? Status { get; protected set; }
    public ReactivePropertySlim<Bitmap>? Source { get; protected set; }
    public ReactivePropertySlim<int>? Position { get; protected set; }
    public ReadOnlyReactivePropertySlim<int>? Count { get; protected set; }
}

public class PreviewWindowModel : PreviewWindowModelBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly PreviewsViewer _previewsViewer;
    private readonly IApplicationDispatcher _applicationDispatcher;
    private readonly IBytesPool _bytesPool;

    private ImmutableList<IFile> _files = ImmutableList<IFile>.Empty;
    private int _initPosition;

    private Size _size;

    private FuncDebouncer<int> _loadPreviewDebouncer;

    private readonly ReactivePropertySlim<bool> _isBusy;
    private readonly ReactivePropertySlim<int> _count;

    private readonly CompositeDisposable _disposable = new();

    public PreviewWindowModel(UiStatus uiStatus, PreviewsViewer previewsViewer, IApplicationDispatcher applicationDispatcher, IBytesPool bytesPool)
    {
        _previewsViewer = previewsViewer;
        _applicationDispatcher = applicationDispatcher;
        _bytesPool = bytesPool;

        this.Status = uiStatus.PicturePreview ??= new PreviewWindowStatus();

        _loadPreviewDebouncer = new FuncDebouncer<int>(this.LoadPreviewAsync);

        _isBusy = new ReactivePropertySlim<bool>(false).AddTo(_disposable);
        _count = new ReactivePropertySlim<int>(0).AddTo(_disposable);

        this.Source = new ReactivePropertySlim<Bitmap>().AddTo(_disposable);
        this.Position = new ReactivePropertySlim<int>().AddTo(_disposable);
        this.Position.Subscribe(_loadPreviewDebouncer.Signal).AddTo(_disposable);
        this.Count = _count.ToReadOnlyReactivePropertySlim().AddTo(_disposable);
    }

    public async ValueTask InitializeAsync(IEnumerable<IFile> files, int initPosition, CancellationToken cancellationToken = default)
    {
        _files = files.ToImmutableList();
        _initPosition = initPosition;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();

        await _loadPreviewDebouncer.DisposeAsync();
        await _previewsViewer.DisposeAsync();
    }

    public async void NotifyWindowsLoaded()
    {
        _count.Value = _files.Count;
        this.Position!.Value = _initPosition;

        await _previewsViewer.LoadAsync(_files, 10, 10, (int)_size.Width, (int)_size.Height);
        _loadPreviewDebouncer.Signal(this.Position!.Value);
    }

    public async void NotifyNext()
    {
        if (_loadPreviewDebouncer.IsRunning) return;
        if (this.Position!.Value + 1 < _previewsViewer.Files.Count) this.Position!.Value++;
    }

    public async void NotifyPrev()
    {
        if (_loadPreviewDebouncer.IsRunning) return;
        if (this.Position!.Value - 1 >= 0) this.Position!.Value--;
    }

    public async void NotifyImageSizeChanged(Size newSize)
    {
        _size = newSize;

        if (!_previewsViewer.IsLoaded) return;

        _previewsViewer.Resize((int)_size.Width, (int)_size.Height);
        _loadPreviewDebouncer.Signal(this.Position!.Value);
    }

    private async Task LoadPreviewAsync(int position, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        if (!_previewsViewer.IsLoaded) return;

        try
        {
            await _applicationDispatcher.InvokeAsync(() =>
            {
                _isBusy!.Value = true;
            }, DispatcherPriority.Background).ConfigureAwait(false);

            var bytes = await _previewsViewer.GetPreviewAsync(position, cancellationToken).ConfigureAwait(false);

            using (var stream = new RecyclableMemoryStream(_bytesPool))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                stream.Seek(0, SeekOrigin.Begin);

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    this.Source?.Value?.Dispose();
                    var source = new Bitmap(stream);
                    this.Source!.Value = source;
                }, DispatcherPriority.Background, cancellationToken).ConfigureAwait(false);
            }

            await _applicationDispatcher.InvokeAsync(() =>
            {
                _isBusy!.Value = false;
            }, DispatcherPriority.Background).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
    }
}
