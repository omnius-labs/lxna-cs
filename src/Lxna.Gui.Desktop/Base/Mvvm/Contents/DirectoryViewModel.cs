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
using Omnix.Network;

namespace Lxna.Gui.Desktop.Base.Contents
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

        public DirectoryModel Model { get; }

        public ReadOnlyReactivePropertySlim<string> Name { get; }
        public ReadOnlyReactiveCollection<DirectoryViewModel> Children { get; }

        public override bool TryAdd(object value)
        {
            throw new NotImplementedException();
        }

        public override bool TryRemove(object value)
        {
            throw new NotImplementedException();
        }

        public OmniAddress GetAddress()
        {
            var parent = this.Parent as DirectoryViewModel;

            if (parent is null)
            {
                return this.Model.Name;
            }
            else
            {
                var parentAddress = parent.GetAddress();
                return OmniAddress.Combine(parentAddress, this.Model.Name);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposable.Dispose();
            }
        }
    }
}
