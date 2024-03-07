using Avalonia.Media.Imaging;
using Omnius.Core;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Ui.Desktop.Shared;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public abstract class PicturePreviewWindowModelBase : AsyncDisposableBase
{
    public abstract ValueTask InitializeAsync(IFile file, CancellationToken cancellationToken = default);

    public PicturePreviewWindowStatus? Status { get; protected set; }
    public ReactivePropertySlim<Bitmap>? Source { get; protected set; }
    public AsyncReactiveCommand? OkCommand { get; protected set; }
    public AsyncReactiveCommand? CancelCommand { get; protected set; }
}

public class PicturePreviewWindowModel : PicturePreviewWindowModelBase
{
    private readonly ImageConverter _imageConverter;
    private readonly IBytesPool _bytesPool;

    private readonly CompositeDisposable _disposable = new();

    public PicturePreviewWindowModel(ImageConverter imageConverter, IBytesPool bytesPool, UiStatus uiStatus)
    {
        _imageConverter = imageConverter;
        _bytesPool = bytesPool;

        this.Status = uiStatus.PicturePreview ??= new PicturePreviewWindowStatus();
        this.Source = new ReactivePropertySlim<Bitmap>().AddTo(_disposable);
    }

    public override async ValueTask InitializeAsync(IFile file, CancellationToken cancellationToken = default)
    {
        using (var inStream = await file.GetStreamAsync(cancellationToken))
        using (var outStream = new RecyclableMemoryStream(_bytesPool))
        {
            await _imageConverter.ConvertAsync(inStream, outStream, ImageFormatType.Png, cancellationToken: cancellationToken);
            outStream.Seek(0, SeekOrigin.Begin);

            var source = new Bitmap(outStream);
            this.Source!.Value = source;
            _disposable.Add(source);
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }
}
