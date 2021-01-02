using System;
using System.Reactive.Disposables;
using Omnius.Lxna.Components;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Models.Primitives;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.Interactors.Models
{
    public sealed class DirectoryViewModel : TreeViewModelBase
    {
        private readonly CompositeDisposable _disposable = new();
        private readonly IFileSystem _fileSystem;

        public DirectoryViewModel(TreeViewModelBase? parent, DirectoryModel model, IFileSystem fileSystem)
            : base(parent)
        {
            this.Model = model;
            _fileSystem = fileSystem;

            this.Name = this.Model.ObserveProperty(n => n.Name).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
            this.Children = this.Model.Children.ToReadOnlyReactiveCollection(n => new DirectoryViewModel(this, n, _fileSystem)).AddTo(_disposable);
            this.IsExpanded = new ReactiveProperty<bool>().AddTo(_disposable);
            this.IsExpanded.Subscribe(value => this.OnIsExpanded(value)).AddTo(_disposable);

            if (model.Path == NestedPath.Empty)
            {
                return;
            }

            this.Model.Children.Add(new DirectoryModel(NestedPath.Empty, _fileSystem));
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

        public ReactiveProperty<bool> IsExpanded { get; }

        private void OnIsExpanded(bool value)
        {
            if (!value) return;

            this.Model.RefreshChildren();
        }

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
