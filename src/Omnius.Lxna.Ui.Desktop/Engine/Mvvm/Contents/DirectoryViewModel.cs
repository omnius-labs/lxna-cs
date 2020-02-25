using System;
using System.Reactive.Disposables;
using Lxna.Gui.Desktop.Models;
using Omnius.Core.Avalonia.Models.Primitives;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Lxna.Gui.Desktop.Core.Contents
{
    sealed class DirectoryViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();

        public DirectoryViewModel(TreeViewModelBase? parent, DirectoryModel model) : base(parent)
        {
            this.Model = model;

            this.Name = this.Model.ObserveProperty(n => n.Name).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
            this.Children = this.Model.Children.ToReadOnlyReactiveCollection(n => new DirectoryViewModel(this, n)).AddTo(_disposable);
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _disposable.Dispose();
            }
        }

        public DirectoryModel Model { get; }

        public ReadOnlyReactivePropertySlim<string?> Name { get; }
        public ReadOnlyReactiveCollection<DirectoryViewModel> Children { get; }

        public override bool TryAdd(object value)
        {
            throw new NotImplementedException();
        }

        public override bool TryRemove(object value)
        {
            throw new NotImplementedException();
        }
    }
}
