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

namespace Lxna.Gui.Desktop.Base.Contents
{
    sealed class FileViewModel : BindableBase
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

        bool _isEffectivelyVisible;
        public bool IsEffectivelyVisible
        {
            get
            {
                return _isEffectivelyVisible;
            }
            set
            {
                _isEffectivelyVisible = value;
            }
        }
    }
}
