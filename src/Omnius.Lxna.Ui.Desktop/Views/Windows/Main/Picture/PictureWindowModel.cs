using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Omnius.Core;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Resources;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.Windows.Views.Main.Picture
{
    public class PictureWindowModel : AsyncDisposableBase
    {
        private readonly AppState _state;
        private readonly NestedPath _path;

        private readonly CompositeDisposable _disposable = new();

        public PictureWindowModel(AppState state, NestedPath path)
        {
            _state = state;
            _path = path;

            this.Source = new ReactivePropertySlim<Bitmap>().AddTo(_disposable);

            this.Init();
        }

        private async void Init()
        {
            using var stream = await _state.GetFileSystem().GetFileStreamAsync(_path);
            var source = new Bitmap(stream);
            this.Source.Value = source;
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _disposable.Dispose();
        }

        public ReactivePropertySlim<Bitmap> Source { get; }
    }
}
