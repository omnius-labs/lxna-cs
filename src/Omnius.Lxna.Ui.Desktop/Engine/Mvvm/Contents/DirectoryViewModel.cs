using System;
using System.IO;
using System.Reactive.Disposables;
using Lxna.Gui.Desktop.Models;
using Omnius.Core.Avalonia.Models.Primitives;
using Omnius.Core.Network;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Lxna.Gui.Desktop.Core.Contents
{
    public sealed class DirectoryViewModel : TreeViewModelBase
    {
        private CompositeDisposable _disposable = new CompositeDisposable();

        public DirectoryViewModel(TreeViewModelBase? parent, DirectoryModel model) : base(parent)
        {
            this.Model = model;

            this.Name = this.Model.ObserveProperty(n => n.Name).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
            this.Children = this.Model.Children.ToReadOnlyReactiveCollection(n => new DirectoryViewModel(this, n)).AddTo(_disposable);
            this.IsExpanded = new ReactiveProperty<bool>().AddTo(_disposable);
            this.IsExpanded.Subscribe(value => this.OnIsExpanded(value)).AddTo(_disposable);

            if (model.Path.Value == "")
            {
                return;
            }

            this.Model.Children.Add(new DirectoryModel(new OmniPath("")));
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

            if(!OmniPath.Windows.TryDecoding(this.Model.Path, out var path))
            {
                return;
            }

            this.Model.Children.Clear();

            foreach (var directoryPath in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
            {
                this.Model.Children.Add(new DirectoryModel(OmniPath.FromWindowsPath(directoryPath)));
            }
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
