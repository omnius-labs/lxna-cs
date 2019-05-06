using System;
using System.Collections.Generic;
using System.Text;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Omnix.Avalonia;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnix.Base;
using Avalonia.Media.Imaging;

namespace Lxna.Gui.Desktop.Windows
{
    sealed class FileViewModel : DisposableBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public FileViewModel(FileModel model)
        {
            this.Model = model;

            this.Name = this.Model.ObserveProperty(n => n.Name).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
        }

        public FileModel Model { get; }

        public ReadOnlyReactivePropertySlim<string> Name { get; }

        public Bitmap? Thumbnail
        {
            get
            {
                return this.Model.Thumbnail;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _disposable.Dispose();
            }
        }
    }
}
