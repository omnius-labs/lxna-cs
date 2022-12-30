using Avalonia.Media.Imaging;
using Omnius.Core;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Axis.Ui.Desktop.Windows.Dialogs.PicturePreview;

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
    private readonly CompositeDisposable _disposable = new();

    public PicturePreviewWindowModel(UiStatus uiStatus)
    {
        this.Status = uiStatus.PicturePreview ??= new PicturePreviewWindowStatus();
        this.Source = new ReactivePropertySlim<Bitmap>().AddTo(_disposable);
    }

    public override async ValueTask InitializeAsync(IFile file, CancellationToken cancellationToken = default)
    {
        using var stream = await file.GetStreamAsync(cancellationToken);
        var source = new Bitmap(stream);
        this.Source!.Value = source;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }
}
