using System.Collections.Immutable;
using Avalonia;
using Avalonia.Media.Imaging;
using Omnius.Core;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Storage;
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

    private readonly ImageConverter _imageConverter;
    private readonly IBytesPool _bytesPool;

    private ImmutableArray<IFile> _files;
    private int _position;
    private Size _imageSize;

    private readonly CompositeDisposable _disposable = new();

    public PreviewWindowModel(ImageConverter imageConverter, IBytesPool bytesPool, UiStatus uiStatus)
    {
        _imageConverter = imageConverter;
        _bytesPool = bytesPool;

        this.Status = uiStatus.PicturePreview ??= new PreviewWindowStatus();
        this.Source = new ReactivePropertySlim<Bitmap>().AddTo(_disposable);
    }

    public override async ValueTask InitializeAsync(IEnumerable<IFile> files, int position, CancellationToken cancellationToken = default)
    {
        _files = files.ToImmutableArray();
        _position = position;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }

    public async void NotifyNext()
    {
        if (_position + 1 < _files.Length)
        {
            _position++;
            await this.LoadAsync();
        }
    }

    public async void NotifyPrev()
    {
        if (_position - 1 >= 0)
        {
            _position--;
            await this.LoadAsync();
        }
    }

    public async void NotifyImageSizeChanged(Size newSize)
    {
        _imageSize = newSize;

        await this.LoadAsync();
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var file = _files[_position];

        try
        {
            using (var inStream = await file.GetStreamAsync(cancellationToken))
            using (var outStream = new RecyclableMemoryStream(_bytesPool))
            {
                await _imageConverter.ConvertAsync(inStream, outStream, ImageResizeType.Min, (int)_imageSize.Width, (int)_imageSize.Height, ImageFormatType.Png, cancellationToken: cancellationToken);
                outStream.Seek(0, SeekOrigin.Begin);

                this.Source?.Value?.Dispose();
                var source = new Bitmap(outStream);
                this.Source!.Value = source;
            }
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
    }
}
