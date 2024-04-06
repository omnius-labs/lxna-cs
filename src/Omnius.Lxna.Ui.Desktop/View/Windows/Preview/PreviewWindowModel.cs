using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
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
    public ReactivePropertySlim<int>? Position { get; protected set; }
    public ReactivePropertySlim<int>? Count { get; protected set; }
}

public class PreviewWindowModel : PreviewWindowModelBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly PreviewsViewer _previewsViewer;
    private readonly IApplicationDispatcher _applicationDispatcher;
    private readonly IBytesPool _bytesPool;

    private Size _size;

    private FuncDebouncer<int> _loadPreviewDebouncer;

    private readonly CompositeDisposable _disposable = new();

    public PreviewWindowModel(UiStatus uiStatus, PreviewsViewer previewsViewer, IApplicationDispatcher applicationDispatcher, IBytesPool bytesPool)
    {
        _previewsViewer = previewsViewer;
        _applicationDispatcher = applicationDispatcher;
        _bytesPool = bytesPool;

        this.Status = uiStatus.PicturePreview ??= new PreviewWindowStatus();

        _loadPreviewDebouncer = new FuncDebouncer<int>(this.LoadPreviewAsync);

        this.Source = new ReactivePropertySlim<Bitmap>().AddTo(_disposable);
        this.Position = new ReactivePropertySlim<int>().AddTo(_disposable);
        this.Position.Subscribe(_loadPreviewDebouncer.Signal).AddTo(_disposable);
        this.Count = new ReactivePropertySlim<int>().AddTo(_disposable);
    }

    public override async ValueTask InitializeAsync(IEnumerable<IFile> files, int position, CancellationToken cancellationToken = default)
    {
        await _previewsViewer.LoadAsync(files, 10, 10, cancellationToken).ConfigureAwait(false);

        this.Position!.Value = position;
        this.Count!.Value = files.Count();
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();

        await _loadPreviewDebouncer.DisposeAsync();
        await _previewsViewer.DisposeAsync();
    }

    public async void NotifyNext()
    {
        if (this.Position!.Value + 1 < _previewsViewer.Files.Count) this.Position!.Value++;
    }

    public async void NotifyPrev()
    {
        if (this.Position!.Value - 1 >= 0) this.Position!.Value--;
    }

    public async void NotifyImageSizeChanged(Size newSize)
    {
        _size = newSize;
        _previewsViewer.SetSize((int)_size.Width, (int)_size.Height);
        _loadPreviewDebouncer.Signal(this.Position!.Value);
    }

    private async Task LoadPreviewAsync(int position, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        try
        {
            var bytes = await _previewsViewer.GetPreviewAsync(position, cancellationToken).ConfigureAwait(false);

            using (var stream = new RecyclableMemoryStream(_bytesPool))
            {
                await stream.WriteAsync(bytes, cancellationToken);
                stream.Seek(0, SeekOrigin.Begin);

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    this.Source?.Value?.Dispose();
                    var source = new Bitmap(stream);
                    this.Source!.Value = source;
                }, DispatcherPriority.Background, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
    }
}
