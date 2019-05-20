using System;
using System.Collections.Generic;
using System.Text;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Omnix.Base;
using Lxna.Gui.Desktop.Models;
using Lxna.Gui.Desktop.Base.Mvvm.Primitives;
using Avalonia.Media.Imaging;

namespace Lxna.Gui.Desktop.Base.Contents
{
    sealed class FileViewModel : BindableBase
    {
        private readonly Action<FileViewModel> _callback;

        private Bitmap? _thumbnail = null;

        private CompositeDisposable _disposable = new CompositeDisposable();

        private volatile bool _disposed;

        public FileViewModel(FileModel model, Action<FileViewModel> callback)
        {
            _callback = callback;

            this.Model = model;
            this.Name = this.Model.ObserveProperty(n => n.Name).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
        }

        public FileModel Model { get; }

        public ReadOnlyReactivePropertySlim<string> Name { get; }

        public Bitmap? Thumbnail
        {
            get
            {
                if (_thumbnail == null)
                {
                    _callback?.Invoke(this);
                }

                return _thumbnail;
            }
        }

        public void SetThumbnail(Bitmap thumbnail)
        {
            _thumbnail = thumbnail;
            base.OnPropertyChanged(nameof(this.Thumbnail));
        }
    }
}
