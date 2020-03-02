using System;
using System.Reactive.Disposables;
using Avalonia.Media.Imaging;
using Lxna.Gui.Desktop.Models;
using Omnius.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Lxna.Gui.Desktop.Core.Contents
{
    public  sealed class FileViewModel : DisposableBase
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public FileViewModel(FileModel model)
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

        public FileModel Model { get; }

        public ReadOnlyReactivePropertySlim<string?> Name { get; }
        public ReadOnlyReactivePropertySlim<Bitmap?> Thumbnail { get; }
    }
}
