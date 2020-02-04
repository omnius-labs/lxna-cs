using System;
using System.Collections.Generic;
using System.Text;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnix.Base;
using Lxna.Gui.Desktop.Models;
using Avalonia.Media.Imaging;

namespace Lxna.Gui.Desktop.Core.Contents
{
    sealed class FileViewModel : DisposableBase
    {
        private readonly Action<FileViewModel> _callback;

        private Bitmap? _thumbnail = null;

        private CompositeDisposable _disposable = new CompositeDisposable();

        public FileViewModel(FileModel model)
        {
            this.Model = model;
            this.Name = this.Model.ObserveProperty(n => n.Name).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
            this.Thumbnail = new ReactiveProperty<Bitmap?>().AddTo(_disposable);
        }

        public FileModel Model { get; }

        public ReadOnlyReactivePropertySlim<string> Name { get; }

        public ReactiveProperty<Bitmap?> Thumbnail { get; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposable.Dispose();
            }
        }
    }
}
