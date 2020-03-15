using System;
using System.Reactive.Disposables;
using Avalonia.Media.Imaging;
using Omnius.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.Engine.Models
{
    public sealed class ItemViewModel : DisposableBase
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public ItemViewModel(ItemModel model)
        {
            this.Model = model;
            this.Name = this.Model.ObserveProperty(n => n.Name).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
            this.Thumbnail = this.Model.ObserveProperty(n => n.Thumbnail).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _disposable.Dispose();
            }
        }

        public ItemModel Model { get; }

        public ReadOnlyReactivePropertySlim<string?> Name { get; }
        public ReadOnlyReactivePropertySlim<Bitmap?> Thumbnail { get; }

        public bool IsShown { get; set; } = false;
        public int Index { get; set; } = -1;
    }
}
