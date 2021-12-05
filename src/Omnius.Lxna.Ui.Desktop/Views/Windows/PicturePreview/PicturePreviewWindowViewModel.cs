using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Omnius.Core;
using Omnius.Lxna.Components;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.Windows;

public class PicturePreviewWindowViewModel : AsyncDisposableBase
{
    private readonly UiState _uiState;
    private readonly IFileSystem _fileSystem;

    private NestedPath? _path;

    private readonly CompositeDisposable _disposable = new();

    public PicturePreviewWindowViewModel(UiState uiState, IFileSystem fileSystem)
    {
        _uiState = uiState;
        _fileSystem = fileSystem;

        this.Source = new ReactivePropertySlim<Bitmap>().AddTo(_disposable);
    }

    public async ValueTask LoadAsync(NestedPath path)
    {
        _path = path;
        using var stream = await _fileSystem.GetFileStreamAsync(_path);
        var source = new Bitmap(stream);
        this.Source.Value = source;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }

    public ReactivePropertySlim<Bitmap> Source { get; }
}
