using Avalonia;
using Avalonia.Media.Imaging;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Ui.Desktop.Service.Internal;
using Omnius.Lxna.Ui.Desktop.Service.Preview;
using Omnius.Lxna.Ui.Desktop.Shared;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public abstract class PreviewWindowModelBase : AsyncDisposableBase
{
    public abstract ValueTask InitializeAsync(IEnumerable<IFile> files, int position, CancellationToken cancellationToken = default);

    public PreviewWindowStatus? Status { get; protected set; }
    public ReactivePropertySlim<Bitmap>? Source { get; protected set; }
    public AsyncReactiveCommand? OkCommand { get; protected set; }
    public AsyncReactiveCommand? CancelCommand { get; protected set; }
}

public class PreviewWindowModel : PreviewWindowModelBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly PreviewsViewer _previewsViewer;
    private readonly IApplicationDispatcher _applicationDispatcher;
    private readonly IBytesPool _bytesPool;

    private int _currentPosition;
    private Size _size;

    private FunctionDebouncer<int> _onLoadPreviewDebouncer;

    private readonly CompositeDisposable _disposable = new();

    public PreviewWindowModel(UiStatus uiStatus, PreviewsViewer previewsViewer, IApplicationDispatcher applicationDispatcher, IBytesPool bytesPool)
    {
        _previewsViewer = previewsViewer;
        _applicationDispatcher = applicationDispatcher;
        _bytesPool = bytesPool;

        this.Status = uiStatus.PicturePreview ??= new PreviewWindowStatus();

        _onLoadPreviewDebouncer = new FunctionDebouncer<int>(this.LoadPreviewAsync);

        this.Source = new ReactivePropertySlim<Bitmap>().AddTo(_disposable);
    }

    public override async ValueTask InitializeAsync(IEnumerable<IFile> files, int position, CancellationToken cancellationToken = default)
    {
        await _previewsViewer.LoadAsync(files, 10, 10, cancellationToken).ConfigureAwait(false);

        _currentPosition = position;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }

    public async void NotifyNext()
    {
        if (_currentPosition + 1 < _previewsViewer.Files.Count)
        {
            _onLoadPreviewDebouncer.Call(_currentPosition + 1);
        }
    }

    public async void NotifyPrev()
    {
        if (_currentPosition - 1 >= 0)
        {
            _onLoadPreviewDebouncer.Call(_currentPosition - 1);
        }
    }

    public async void NotifyImageSizeChanged(Size newSize)
    {
        _size = newSize;
        _previewsViewer.SetSize((int)_size.Width, (int)_size.Height);
        _onLoadPreviewDebouncer.Call(_currentPosition);
    }

    private async Task LoadPreviewAsync(int position)
    {
        await Task.Delay(1).ConfigureAwait(false);

        try
        {
            var bytes = await _previewsViewer.GetPreviewAsync(position).ConfigureAwait(false);

            using (var stream = new RecyclableMemoryStream(_bytesPool))
            {
                stream.Write(bytes.Span);
                stream.Seek(0, SeekOrigin.Begin);

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    this.Source?.Value?.Dispose();
                    var source = new Bitmap(stream);
                    this.Source!.Value = source;
                    _currentPosition = position;
                }).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
    }
}
