using System.Reactive.Disposables;
using Avalonia.Media.Imaging;
using Omnius.Lxna.Components.Storage;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public class PreviewWindowDesignModel : PreviewWindowModelBase
{
    private readonly ReactivePropertySlim<int> _count;

    private readonly CompositeDisposable _disposable = new();

    public PreviewWindowDesignModel()
    {
        this.Status = new Shared.PreviewWindowStatus();

        _count = new ReactivePropertySlim<int>(32).AddTo(_disposable);

        this.Source = new ReactivePropertySlim<Bitmap>().AddTo(_disposable);
        this.Position = new ReactivePropertySlim<int>(16).AddTo(_disposable);
        this.Count = _count.ToReadOnlyReactivePropertySlim().AddTo(_disposable);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }
}
