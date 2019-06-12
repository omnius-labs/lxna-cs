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
    sealed class DirectoryViewModel : DisposableBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();

        private volatile bool _disposed;

        public DirectoryViewModel(DirectoryModel model)
        {
            this.Model = model;

            this.Name = this.Model.ObserveProperty(n => n.Name).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
            this.Children = this.Model.Children.ToReadOnlyReactiveCollection(n => new DirectoryViewModel(n)).AddTo(_disposable);
        }

        public DirectoryModel Model { get; }

        public ReadOnlyReactivePropertySlim<string> Name { get; }
        public ReadOnlyReactiveCollection<DirectoryViewModel> Children { get; }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (disposing)
            {
                _disposable.Dispose();
            }
        }
    }
}
